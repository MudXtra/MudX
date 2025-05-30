
export async function injectCssFromFile() {
    // Look for an existing <link> tag with data-mudx-css="true"
    let linkTag = document.querySelector('link[data-mudx-css="true"]');

    if (linkTag) {
        // If it exists, update the href
        linkTag.href = cssPath;
    } else {
        // Otherwise, create a new <link> tag
        linkTag = document.createElement("link");
        linkTag.setAttribute("data-mudx-css", "true");
        linkTag.rel = "stylesheet";
        linkTag.href = "./_content/MudX/mudx.css";

        document.head.appendChild(linkTag);
    }
}

export async function injectJsFromFile() {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-mudx-js]")) {
            // Already loaded
            return resolve();
        }

        const script = document.createElement("script");
        script.setAttribute("data-mudx-js", "true");
        script.type = "text/javascript";
        script.src = "./_content/MudX/mudx.min.js";

        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load mudx.min.js"));

        document.head.appendChild(script);
    });
}

export async function initialize() {
    try {
        await injectCssFromFile();
        await injectJsFromFile();
        return true;
    } catch (error) {
        console.error("Provider Initialization failed:", error);
        return false;
    }
}