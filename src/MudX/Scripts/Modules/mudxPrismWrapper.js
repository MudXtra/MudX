
export async function injectCssFromFile(cssPath) {
    // Look for an existing <link> tag with data-prism="true"
    let linkTag = document.querySelector('link[data-prism="true"]');

    if (linkTag) {
        // If it exists, update the href
        linkTag.href = cssPath;
    } else {
        // Otherwise, create a new <link> tag
        linkTag = document.createElement("link");
        linkTag.setAttribute("data-prism", "true");
        linkTag.rel = "stylesheet";
        linkTag.href = cssPath;
        
        document.head.appendChild(linkTag);
    }
}

export async function loadPrism() {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-prism]")) {
            // Already loaded
            return resolve();
        }

        const script = document.createElement("script");
        script.setAttribute("data-prism", "true");
        script.type = "text/javascript";
        script.src = "./_content/MudX/prism/prism.js";

        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load Prism.js"));

        document.head.appendChild(script);
    });
}

export async function initialize(cssPath) {
    try {
        await injectCssFromFile(cssPath);
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