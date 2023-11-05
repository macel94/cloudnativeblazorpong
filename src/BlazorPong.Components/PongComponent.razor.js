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
