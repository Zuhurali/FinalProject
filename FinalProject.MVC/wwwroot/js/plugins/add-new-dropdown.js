(function () {
    'use strict';

    var root = document.querySelector('.av-add-new');
    if (!root) {
        return;
    }

    var toggle = root.querySelector('.av-add-new__toggle');
    if (!toggle) {
        return;
    }

    function setOpen(open) {
        root.classList.toggle('is-open', open);
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    toggle.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopPropagation();
        setOpen(!root.classList.contains('is-open'));
    });

    document.addEventListener('click', function (event) {
        if (!root.contains(event.target)) {
            setOpen(false);
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            setOpen(false);
        }
    });
})();
