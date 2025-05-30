
export async function injectCssFromFile(version) {
    // Look for an existing <link> tag with data-mudx-css="true"
    let linkTag = document.querySelector('link[data-mudx-css="true"]');
    const cssPath = `./_content/MudX/mudx.min.css?v=${version}`;
    if (linkTag) {
        // If it exists, update the href
        linkTag.href = cssPath;
    } else {
        // Otherwise, create a new <link> tag
        linkTag = document.createElement("link");
        linkTag.setAttribute("data-mudx-css", "true");
        linkTag.rel = "stylesheet";
        linkTag.href = cssPath;

        document.head.appendChild(linkTag);
    }
}

export async function injectJsFromFile(version) {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-mudx-js]")) {
            // Already loaded
            return resolve();
        }
        const jsPath = `./_content/MudX/mudx.min.js?v=${version}`;
        const script = document.createElement("script");
        script.setAttribute("data-mudx-js", "true");
        script.type = "text/javascript";
        script.src = jsPath;

        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load mudx.min.js"));

        document.head.appendChild(script);
    });
}

export async function initialize(version) {
    try {
        await injectCssFromFile(version);
        await injectJsFromFile(version);
        return true;
    } catch (error) {
        console.error("Provider Initialization failed:", error);
        return false;
    }
}