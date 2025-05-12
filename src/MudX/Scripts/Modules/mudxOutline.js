// mudxOutline.js
export function createScrollSpy() {
    return new MudXScrollSpy();
}

const outlineMap = {};

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

    updatePosition();

    if (!outlineMap[popoverId]) {
        outlineMap[popoverId] = updatePosition;
        window.addEventListener("resize", updatePosition);
    }
}

export function disposePopoverResize(popoverId) {
    const entry = outlineMap[popoverId];
    if (entry) {
        window.removeEventListener("resize", entry);
        delete outlineMap[popoverId];
    }
}
