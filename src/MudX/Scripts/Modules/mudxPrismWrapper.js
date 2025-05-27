let prismLoaded = false;

export async function highlightAll() {
    if (!prismLoaded) {
        await import("../prism/prism.js");
        prismLoaded = true;
    }

    if (window.Prism) {
        window.Prism.highlightAll();
    }
    else {
        console.error("Prism is not loaded.");
    }
}