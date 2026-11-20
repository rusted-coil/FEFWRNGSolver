export function confirmSearch(message) {
    const isPhone = navigator.userAgentData?.mobile ?? /iPhone|iPod|Android.*Mobile/i.test(navigator.userAgent);
    return !isPhone || window.confirm(message);
}
