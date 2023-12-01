export function setOnbeforeunload (instance) {
    window.onbeforeunload = function () {
        instance.invokeMethodAsync('DisposePongComponent');
    };
};

export function unsetOnbeforeunload(instance) {
    window.onbeforeunload = null;
};

export function log(message) {
    console.log(message);
};

export function getContainerHeight() {
    var element = document.getElementById('gamearea');
    if (element) {
        return element.offsetHeight;
    }
    return 0;
}
