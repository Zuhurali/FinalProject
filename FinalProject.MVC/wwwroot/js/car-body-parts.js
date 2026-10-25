(function () {
    function syncGroup(root) {
        var checks = root.querySelectorAll('.av-part-check');
        var stored = root.querySelector('.av-part-stored');
        var otherText = root.querySelector('.av-part-other-text');
        if (!stored) {
            return;
        }

        var selected = [];
        var otherChecked = false;
        checks.forEach(function (input) {
            if (!input.checked) {
                return;
            }
            if (input.classList.contains('av-part-other')) {
                otherChecked = true;
                return;
            }
            selected.push(input.value);
        });

        var other = otherText ? otherText.value.trim() : '';
        if (otherChecked || other) {
            selected.push(other ? ('Other:' + other) : 'Other');
        }

        stored.value = selected.join('|');
    }

    function bindBodyParts() {
        document.querySelectorAll('[data-body-parts]').forEach(function (root) {
            var other = root.querySelector('.av-part-other');
            var otherWrap = other
                ? document.getElementById(other.getAttribute('data-other-wrap') || '')
                : null;
            var otherText = root.querySelector('.av-part-other-text');

            function refreshOther() {
                if (!otherWrap || !other) {
                    return;
                }
                otherWrap.hidden = !other.checked;
                if (!other.checked && otherText) {
                    otherText.value = '';
                }
            }

            root.querySelectorAll('.av-part-check').forEach(function (input) {
                input.addEventListener('change', function () {
                    refreshOther();
                    syncGroup(root);
                });
            });
            if (otherText) {
                otherText.addEventListener('input', function () {
                    if (other && otherText.value.trim() && !other.checked) {
                        other.checked = true;
                        refreshOther();
                    }
                    syncGroup(root);
                });
            }

            refreshOther();
            syncGroup(root);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindBodyParts);
    } else {
        bindBodyParts();
    }
})();
