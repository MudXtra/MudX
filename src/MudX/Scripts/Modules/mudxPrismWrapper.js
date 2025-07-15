
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
        linkTag.type = "text/css";
        linkTag.rel = "stylesheet";
        linkTag.href = cssPath;
        
        document.head.appendChild(linkTag);
    }
}

export async function loadPrism(packageId) {
    return new Promise((resolve, reject) => {
        if (document.querySelector("script[data-prism]")) {
            // Already loaded
            return resolve();
        }

        const script = document.createElement("script");
        script.setAttribute("data-prism", "true");
        script.type = "text/javascript";
        script.src = `./_content/${packageId}/prism/prism.js`;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load Prism.js"));

        document.head.appendChild(script);
        // wait 150ms to return
        setTimeout(resolve, 150);
    });
}

export async function initialize(cssPath, packageId) {
    try {
        await injectCssFromFile(cssPath);
        await loadPrism(packageId);
        return true;
    } catch (error) {
        console.error("Initialization failed:", error);
        return false;
    }
}

export function highlightElementById(elementId) {
    if (!window.Prism) {
        return;
    }

    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`Element with id '${elementId}' not found.`);
        return;
    }

    window.Prism.highlightAllUnder(element);
}
