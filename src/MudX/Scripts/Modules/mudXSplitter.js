
export async function startDrag(element, pointerId) {
    if (element) {
        element.setPointerCapture(pointerId);
        // ensure the element can receive keyboard down event
        element.focus();
        const rect = element.getBoundingClientRect();
        return [rect.width, rect.height];
    }
    return [window.innerWidth, window.innerHeight];
}

export async function cancelDrag(element, pointerId) {
    if (element) {
        try {
            if (element.hasPointerCapture && element.hasPointerCapture(pointerId)) {
                element.releasePointerCapture(pointerId);
            }

            const focusable = element.querySelector(
                'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
            );
            if (focusable) {
                focusable.focus();
            }
        } catch (e) {
            // Optional: log the error if needed
            console.warn("cancelDrag error:", e);
        }
    }
}
