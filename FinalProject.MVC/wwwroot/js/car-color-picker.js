(function () {
    function expandHex(hex) {
        if (!hex) {
            return '';
        }
        var text = hex.trim();
        if (text.charAt(0) !== '#') {
            text = '#' + text;
        }
        if (/^#[0-9a-fA-F]{3}$/.test(text)) {
            return ('#' + text[1] + text[1] + text[2] + text[2] + text[3] + text[3]).toUpperCase();
        }
        if (/^#[0-9a-fA-F]{6}$/.test(text)) {
            return text.toUpperCase();
        }
        return '';
    }

    function presetsFromDom() {
        return Array.prototype.slice.call(document.querySelectorAll('.av-color-swatch[data-hex]'))
            .filter(function (button) {
                return (button.getAttribute('data-color') || '') !== 'Other'
                    && (button.getAttribute('data-hex') || '').length > 0;
            })
            .map(function (button) {
                return {
                    name: button.getAttribute('data-color') || '',
                    hex: expandHex(button.getAttribute('data-hex') || ''),
                    button: button
                };
            });
    }

    function findPresetByHex(hex, presets) {
        var normalized = expandHex(hex);
        if (!normalized) {
            return null;
        }
        return presets.find(function (item) { return item.hex === normalized; }) || null;
    }

    function findPresetByName(name, presets) {
        var text = (name || '').trim();
        if (!text) {
            return null;
        }
        return presets.find(function (item) {
            return item.name.toLowerCase() === text.toLowerCase();
        }) || null;
    }

    function bindCarColorPicker() {
        var colorHidden = document.getElementById('Car_Color');
        var colorName = document.getElementById('Car_Color_Name');
        var colorHex = document.getElementById('Car_Color_Hex');
        var colorPicker = document.getElementById('Car_Color_Picker');
        var colorPreview = document.getElementById('Car_Color_Preview');
        var colorNameWrap = document.getElementById('Car_Color_Name_Wrap');
        var colorSelected = document.getElementById('Car_Color_Selected');
        if (!colorHidden || !colorHex || !colorPicker) {
            return;
        }

        var presets = presetsFromDom();
        var otherButton = document.querySelector('.av-color-swatch[data-color="Other"]');

        function setSelectedLabel(name, hex) {
            if (!colorSelected) {
                return;
            }
            if (!name && !hex) {
                colorSelected.textContent = 'No color selected';
                return;
            }
            colorSelected.textContent = hex ? (name + ' (' + hex + ')') : name;
        }

        function markSwatch(name, isCustom) {
            document.querySelectorAll('.av-color-swatch').forEach(function (item) {
                item.classList.remove('is-selected');
            });
            if (isCustom) {
                if (otherButton) {
                    otherButton.classList.add('is-selected');
                }
                return;
            }
            var preset = findPresetByName(name, presets);
            if (preset && preset.button) {
                preset.button.classList.add('is-selected');
            }
        }

        function showCustomName(show) {
            if (!colorNameWrap) {
                return;
            }
            colorNameWrap.hidden = !show;
            if (colorName) {
                colorName.required = show;
            }
        }

        function commit(name, hex) {
            var safeName = (name || '').trim();
            var safeHex = expandHex(hex);
            if (colorName) {
                colorName.value = safeName;
            }
            colorHex.value = safeHex;
            if (safeHex) {
                colorPicker.value = safeHex;
            }
            if (colorPreview) {
                colorPreview.style.background = safeHex || 'transparent';
            }
            colorHidden.value = safeHex ? (safeName + '|' + safeHex) : safeName;
            setSelectedLabel(safeName, safeHex);
        }

        function applyFromSwatch(button) {
            var name = button.getAttribute('data-color') || '';
            var hex = button.getAttribute('data-hex') || '';
            if (name === 'Other') {
                markSwatch('', true);
                showCustomName(true);
                commit('Custom color', colorHex.value || '');
                if (colorName) {
                    colorName.focus();
                    colorName.select();
                }
                return;
            }
            markSwatch(name, false);
            showCustomName(false);
            commit(name, hex);
        }

        function applyFromHexOrPicker(hexValue) {
            var hex = expandHex(hexValue);
            if (!hex) {
                return;
            }
            var preset = findPresetByHex(hex, presets);
            if (preset) {
                markSwatch(preset.name, false);
                showCustomName(false);
                commit(preset.name, preset.hex);
                return;
            }
            var currentName = colorName ? colorName.value.trim() : '';
            var currentPreset = findPresetByName(currentName, presets);
            var nextName = (!currentName || currentPreset || currentName === 'Other')
                ? 'Custom color'
                : currentName;
            markSwatch('', true);
            showCustomName(true);
            commit(nextName, hex);
        }

        document.querySelectorAll('.av-color-swatch').forEach(function (button) {
            button.addEventListener('click', function () {
                applyFromSwatch(button);
            });
        });

        colorHex.addEventListener('input', function () {
            applyFromHexOrPicker(colorHex.value);
        });
        colorHex.addEventListener('change', function () {
            applyFromHexOrPicker(colorHex.value);
        });

        colorPicker.addEventListener('input', function () {
            applyFromHexOrPicker(colorPicker.value);
        });
        colorPicker.addEventListener('change', function () {
            applyFromHexOrPicker(colorPicker.value);
        });

        if (colorName) {
            colorName.addEventListener('input', function () {
                var name = colorName.value.trim();
                var hex = expandHex(colorHex.value);
                var preset = findPresetByName(name, presets);
                if (preset) {
                    markSwatch(preset.name, false);
                    showCustomName(false);
                    commit(preset.name, preset.hex);
                    return;
                }
                markSwatch('', true);
                showCustomName(true);
                commit(name || 'Custom color', hex);
            });
        }

        var initialName = colorName ? colorName.value.trim() : '';
        var initialHex = expandHex(colorHex.value);
        if (!initialName && !initialHex) {
            markSwatch('', false);
            showCustomName(false);
            colorHidden.value = '';
            if (colorPreview) {
                colorPreview.style.background = 'transparent';
            }
            setSelectedLabel('', '');
            return;
        }

        var matched = findPresetByHex(initialHex, presets) || findPresetByName(initialName, presets);
        if (matched) {
            markSwatch(matched.name, false);
            showCustomName(false);
            commit(matched.name, matched.hex || initialHex);
            return;
        }

        markSwatch('', true);
        showCustomName(true);
        commit(initialName || 'Custom color', initialHex);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindCarColorPicker);
    } else {
        bindCarColorPicker();
    }
})();
