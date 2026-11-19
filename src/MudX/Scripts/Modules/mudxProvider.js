
export async function injectCssFromFile(version, isDev, packageId) {
    // Look for an existing <link> tag with data-mudx-css="true"
    let linkTag = document.querySelector('link[data-mudx-css="true"]');
    const cssPath = `./_content/${packageId}/mudx.${!isDev ? 'min.' : ''}css?v=${version}`;
    if (linkTag) {
        // If it exists, update the href
        linkTag.href = cssPath;
    } else {
        // Otherwise, create a new <link> tag
        linkTag = document.createElement("link");
        linkTag.setAttribute("data-mudx-css", "true");
        linkTag.type = "text/css";
        linkTag.rel = "stylesheet";
        linkTag.href = cssPath;

        document.head.appendChild(linkTag);
    }
}

export async function injectJsFromFile(version, isDev, packageId) {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-mudx-js]")) {
            // Already loaded
            return resolve();
        }
        const jsPath = `./_content/${packageId}/mudx.${!isDev ? 'min.' : ''}js?v=${version}`;
        const script = document.createElement("script");
        script.setAttribute("data-mudx-js", "true");
        script.type = "text/javascript";
        script.src = jsPath;

        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load ${script.src}`));

        document.head.appendChild(script);
    });
}

export async function initialize(version, isDev, packageId) {
    try {
        await injectCssFromFile(version, isDev, packageId);
        await injectJsFromFile(version, isDev, packageId);
        return true;
    } catch (error) {
        console.error("Provider Initialization failed:", error);
        return false;
    }
}

// Resolves true once the main mudx script has defined window[globalName]. Components can render before the
// provider's asynchronously injected script has loaded (cold page load), so they wait here before using it.
export async function waitForMudXScript(globalName, timeoutMs) {
    const deadline = Date.now() + timeoutMs;
    while (typeof window[globalName] === "undefined") {
        if (Date.now() >= deadline) {
            return false;
        }
        await new Promise(resolve => setTimeout(resolve, 25));
    }
    return true;
}
