const handledKeys = new Set(["Backspace", "Delete", "ArrowLeft", "ArrowRight"]);
const deleteDedupeWindowMs = 100;
const inputRegistrations = new WeakMap();
const containerStates = new WeakMap();

export function init(dotNetObjRef, container) {
    if (!container) return;

    let state = containerStates.get(container);
    if (!state || state.disposed) {
        state = {
            disposed: false,
            running: false,
            queue: [],
            queuedRepeatKeys: new Set()
        };
        containerStates.set(container, state);
    }

    const inputs = container.querySelectorAll("input");
    inputs.forEach((input) => {
        if (inputRegistrations.has(input)) return;

        const registration = {};
        registration.paste = (event) => handlePaste(event, input, dotNetObjRef);
        registration.keydown = (event) => handleKeyDown(event, input, container, dotNetObjRef, registration);
        registration.beforeinput = (event) => handleBeforeInput(event, input, container, dotNetObjRef, registration);
        input.addEventListener("paste", registration.paste);
        input.addEventListener("keydown", registration.keydown);
        input.addEventListener("beforeinput", registration.beforeinput);
        inputRegistrations.set(input, registration);
    });
}

export function cleanup(container) {
    if (!container) return;

    const state = containerStates.get(container);
    if (state) {
        state.disposed = true;
        state.queue.length = 0;
        state.queuedRepeatKeys.clear();
        containerStates.delete(container);
    }

    const inputs = container.querySelectorAll("input");
    inputs.forEach((input) => {
        const registration = inputRegistrations.get(input);
        if (!registration) return;

        input.removeEventListener("paste", registration.paste);
        input.removeEventListener("keydown", registration.keydown);
        input.removeEventListener("beforeinput", registration.beforeinput);
        inputRegistrations.delete(input);
    });
}

function handlePaste(event, input, dotNetObjRef) {
    if (!event || !input || !dotNetObjRef) return;
    event.preventDefault();

    const paste = (event.clipboardData || window.clipboardData)?.getData("Text");
    if (paste) {
        dotNetObjRef.invokeMethodAsync("ClipboardPasteEvent", input.id, paste);
        input.blur();
    }
}

function handleKeyDown(event, input, container, dotNetObjRef, registration) {
    if (!event || event.isComposing || event.keyCode === 229 || !handledKeys.has(event.key)) return;

    event.preventDefault();
    event.stopImmediatePropagation();

    if (event.key === "Backspace" || event.key === "Delete") {
        registration.deleteKeyDownKey = event.key;
        registration.deleteKeyDownUntil = performance.now() + deleteDedupeWindowMs;
    }

    queueKeyboardEvent(container, input, dotNetObjRef, event.key, event.repeat === true);
}

function handleBeforeInput(event, input, container, dotNetObjRef, registration) {
    if (!event || event.isComposing) return;

    const key = event.inputType === "deleteContentBackward"
        ? "Backspace"
        : event.inputType === "deleteContentForward"
            ? "Delete"
            : null;
    if (!key) return;

    event.preventDefault();
    event.stopImmediatePropagation();

    if (registration.deleteKeyDownKey === key && performance.now() <= registration.deleteKeyDownUntil) return;
    queueKeyboardEvent(container, input, dotNetObjRef, key, false);
}

function queueKeyboardEvent(container, input, dotNetObjRef, key, repeat) {
    const state = containerStates.get(container);
    if (!state || state.disposed) return;

    const repeatKey = repeat ? `${input.id}:${key}` : null;
    if (repeatKey && state.queuedRepeatKeys.has(repeatKey)) return;

    state.queue.push({ container, input, dotNetObjRef, key, repeatKey });
    if (repeatKey) state.queuedRepeatKeys.add(repeatKey);
    void drainKeyboardQueue(state);
}

async function drainKeyboardQueue(state) {
    if (state.running || state.disposed) return;

    state.running = true;
    try {
        while (!state.disposed && state.queue.length > 0) {
            const action = state.queue.shift();
            if (action.repeatKey) state.queuedRepeatKeys.delete(action.repeatKey);

            try {
                await dispatchKeyboardAction(state, action);
            }
            catch (error) {
                if (!state.disposed) {
                    console.error("MudXSecurityCode keyboard bridge failed.", error);
                }
            }
        }
    }
    finally {
        state.running = false;
    }
}

async function dispatchKeyboardAction(state, action) {
    if (state.disposed) return;

    const activeInput = action.container.contains(document.activeElement) && document.activeElement?.tagName === "INPUT"
        ? document.activeElement
        : action.input;
    const inputId = await action.dotNetObjRef.invokeMethodAsync("HandleKeyboardEvent", activeInput.id, action.key);
    if (state.disposed) return;

    await new Promise(resolve => requestAnimationFrame(resolve));
    if (!state.disposed && inputId) {
        focusBlock(action.container, inputId);
    }
}

export function focusBlock(container, inputId) {
    if (!container || !inputId) return;
    const input = container.querySelector("#" + inputId);
    if (input) {
        try {
            input.focus();
            input.select();
        }
        catch { }
    }
}

export function focusNextAfterContainer(container) {
    if (!container) return;
    setTimeout(() => focusNextElement(), 0);
}

function focusNextElement() {
    const focusableSelector = 'a:not([disabled]), button:not([disabled]), input:not([disabled]), textarea:not([disabled]), select:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])';

    if (!document.activeElement) return;

    const container = document.activeElement.form || document;
    const focusableElements = Array.from(container.querySelectorAll(focusableSelector))
        .filter(element => {
            return element.offsetWidth > 0 || element.offsetHeight > 0 || element === document.activeElement;
        });

    const currentIndex = focusableElements.indexOf(document.activeElement);
    const nextIndex = currentIndex + 1;

    if (nextIndex < focusableElements.length) {
        const el = focusableElements[nextIndex];
        if (el) {
            el.focus();
            if (typeof el.select === 'function') {
                el.select();
            }
        }
    }
}
