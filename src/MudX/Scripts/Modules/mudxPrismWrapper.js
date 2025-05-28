
async function injectCssFromFile(cssPath, styleId) {
    const styleTag = document.getElementById(styleId);
    if (!styleTag) {
        console.error("No Style Tag found for code display.")
        return;
    }
    const cssText = await fetch(cssPath).then(res => res.text());
    styleTag.innerHTML = cssText;
}

async function loadPrism() {
    const prismTag = document.querySelector("script[data-prism]");
    if (!prismTag) {
        const script = document.createElement("script");
        script.setAttribute("data-prism", "true");
        script.type = "text/javascript";

        const jsText = await fetch("./_content/MudX/prism/prism.js").then(res => res.text());
        script.text = jsText;

        document.head.appendChild(script);
    }
}

export async function initialize(cssPath, styleId) {
    try {
        await injectCssFromFile(cssPath, styleId);
        await loadPrism();
        return true;
    } catch (error) {
        console.error("Initialization failed:", error);
        return false;
    }
}

export async function highlightElementById(elementId) {
    if (!window.Prism) {
        console.error("Prism is not loaded.");
        return;
    }

    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`Element with id '${elementId}' not found.`);
        return;
    }

    window.Prism.highlightAllUnder(element);
}

export async function copyToClipboard(copyText) {
    try {
        // Focus workaround - ensures document has focus
        if (!document.hasFocus()) {
            window.focus();
        }

        await navigator.clipboard.writeText(copyText);
        return true;
    } catch (error) {
        console.error('Clipboard copy failed:', error);

        // Fallback method for problematic browsers
        try {
            const textarea = document.createElement('textarea');
            textarea.value = copyText;
            textarea.style.position = 'fixed';  // Prevent scrolling
            document.body.appendChild(textarea);
            textarea.select();

            const successful = document.execCommand('copy');
            document.body.removeChild(textarea);

            return successful;
        } catch (fallbackError) {
            console.error('Fallback copy failed:', fallbackError);
            return false;
        }
    }
}