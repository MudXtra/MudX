// Run with an available Playwright installation (no repository dependency changes):
// NODE_PATH=<node_modules> BROWSER=chromium node --test tests/MudX.UnitTests/Scripts/securityCodeFocus.test.cjs
// Repeat with BROWSER=firefox. Optionally set BROWSER_EXECUTABLE to an installed browser.
// bUnit does not execute this browser module; only the .NET response and frame scheduling
// are controlled here. Focus events, native Tab and pointer input use a real browser DOM.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const playwright = require('playwright');

const root = path.resolve(__dirname, '../../..');
const browserName = process.env.BROWSER || 'chromium';
const modules = ['Scripts/Modules/mudxSecurityCode.js', 'wwwroot/modules/mudxSecurityCode.js'];

for (const modulePath of modules) {
    test(`${browserName}: ${modulePath}`, async t => {
        const browser = await playwright[browserName].launch({
            headless: true,
            executablePath: process.env.BROWSER_EXECUTABLE || undefined
        });
        const source = fs.readFileSync(path.join(root, 'src/MudX', modulePath), 'utf8');
        const page = await browser.newPage();
        const setup = async () => {
            await page.goto('about:blank');
            await page.setContent('<button id="before">Before</button><div id="code"><input id="a"><input id="b"><input id="c"></div><button id="after">After</button>');
            await page.evaluate(async source => {
                window.bridge = await import(URL.createObjectURL(new Blob([source], { type: 'text/javascript' })));
                window.calls = [];
                window.releases = [];
                window.frames = [];
                window.requestAnimationFrame = callback => frames.push(callback);
                window.ref = {
                    invokeMethodAsync: (...args) => {
                        calls.push(args);
                        return new Promise(resolve => releases.push(resolve));
                    }
                };
                bridge.init(ref, document.getElementById('code'));
                bridge.init(ref, document.getElementById('code'));
            }, source);
        };
        const resolve = async id => page.evaluate(id => releases.shift()(id), id);
        const frame = async () => page.evaluate(() => frames.shift()());
        const active = async () => page.evaluate(() => document.activeElement.id);
        const moves = [
            ['Tab outside', 'c', 'after', () => page.keyboard.press('Tab')],
            ['Shift+Tab outside', 'a', 'before', () => page.keyboard.press('Shift+Tab')],
            ['Tab inside', 'a', 'b', () => page.keyboard.press('Tab')],
            ['Shift+Tab inside', 'c', 'b', () => page.keyboard.press('Shift+Tab')],
            ['pointer outside', 'a', 'after', () => page.locator('#after').click()],
            ['pointer inside', 'a', 'b', () => page.locator('#b').click()],
            ['consumer outside', 'a', 'after', () => page.locator('#after').focus()],
            ['consumer inside', 'a', 'b', () => page.evaluate(() => bridge.focusBlock(document.getElementById('code'), 'b'))],
            ['leave and return', 'a', 'a', () => page.evaluate(() => { document.getElementById('after').focus(); document.getElementById('a').focus(); })],
            ['blur', 'a', '', () => page.evaluate(() => document.activeElement.blur())]
        ];
        try {
            for (const phase of ['interop', 'animation frame']) {
                for (const [name, start, expected, move] of moves) {
                    await t.test(`${name} during ${phase} invalidates pending and queued focus`, async () => {
                        await setup();
                        await page.locator('#' + start).focus();
                        await page.keyboard.press('Backspace');
                        await page.keyboard.press('Delete');
                        assert.equal(await page.evaluate(() => calls.length), 1, 'dispatch remains serialized');
                        if (phase === 'animation frame') await resolve('c');
                        await move();
                        assert.equal(await active(), expected, 'new focus actually took effect');
                        if (phase === 'interop') await resolve('c');
                        await frame();
                        assert.equal(await active(), expected, 'pending action must not steal focus');
                        assert.deepEqual(await page.evaluate(() => calls.map(x => x.slice(1))), [[start, 'Backspace'], [start, 'Delete']], 'queued mutation is retained on its original input');
                        await resolve('c');
                        await frame();
                        assert.equal(await active(), expected, 'queued action must not steal focus either');
                        // New keyboard intent after the focus move still owns its focus response.
                        await page.locator('#a').focus();
                        await page.keyboard.press('ArrowRight');
                        await resolve('b');
                        await frame();
                        assert.equal(await active(), 'b');
                    });
                }
            }
            await t.test('bridge-owned focus preserves queued dispatch-time targeting and repeat coalescing', async () => {
                await setup();
                await page.locator('#a').focus();
                await page.keyboard.press('ArrowRight');
                await page.keyboard.down('Backspace');
                for (let i = 0; i < 8; i++) await page.keyboard.down('Backspace');
                await page.keyboard.up('Backspace');
                assert.equal(await page.evaluate(() => calls.length), 1);
                for (const target of ['b', 'a', 'c']) {
                    await resolve(target);
                    await frame();
                    assert.equal(await active(), target);
                }
                assert.deepEqual(await page.evaluate(() => calls.map(x => x.slice(1))), [['a', 'ArrowRight'], ['b', 'Backspace'], ['a', 'Backspace']]);
            });
            await t.test('beforeinput-only deletion yields to newer focus', async () => {
                await setup();
                await page.locator('#a').focus();
                await page.evaluate(() => document.activeElement.dispatchEvent(new InputEvent('beforeinput', { inputType: 'deleteContentBackward', bubbles: true, cancelable: true })));
                await page.locator('#after').focus();
                await resolve('b');
                await frame();
                assert.equal(await active(), 'after');
                assert.deepEqual(await page.evaluate(() => calls.map(x => x.slice(1))), [['a', 'Backspace']]);
            });
            await t.test('cleanup cancels pending focus and queue; reinitialization has fresh ownership', async () => {
                await setup();
                await page.locator('#a').focus();
                await page.keyboard.press('Backspace');
                await page.keyboard.press('Delete');
                await page.evaluate(() => bridge.cleanup(document.getElementById('code')));
                await resolve('b');
                assert.equal(await active(), 'a');
                assert.equal(await page.evaluate(() => calls.length), 1);
                assert.equal(await page.evaluate(() => frames.length), 0);
                await page.evaluate(() => bridge.init(ref, document.getElementById('code')));
                await page.keyboard.press('ArrowRight');
                await resolve('b');
                await frame();
                assert.equal(await active(), 'b');
            });
        }
        finally {
            await browser.close();
        }
    });
}
