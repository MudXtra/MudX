
const observerMap = new Map();

export function getViewportCorners(element, popoverId, isLeft) {
    const getAbsolutePosition = (el) => {
        let x = 0, y = 0;
        let current = el;
        while (current) {
            x += current.offsetLeft - current.scrollLeft;
            y += current.offsetTop - current.scrollTop;
            current = current.offsetParent;
        }
        return { x, y };
    };

    const updatePosition = () => {
        const marginLeft = parseInt(window.getComputedStyle(element).marginLeft, 10);
        const { x, y } = getAbsolutePosition(element);
        const popover = document.getElementById(popoverId);
        if (popover) {
            const adjustedX = isLeft ? x - marginLeft : x + element.offsetWidth;
            popover.style.left = `${adjustedX}px`;
            popover.style.top = `${y}px`;
        }
    };

    disposePopoverResize(popoverId);
    updatePosition();
    window.addEventListener("resize", updatePosition);
    observerMap.set(popoverId, updatePosition);
    return true;
};

export function disposePopoverResize(popoverId) {
    if (observerMap.has(popoverId)) {
        const entry = observerMap.get(popoverId);
        window.removeEventListener("resize", entry);
        observerMap.delete(popoverId);
    }
};