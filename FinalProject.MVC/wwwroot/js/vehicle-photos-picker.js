/**
 * Vehicle Photos picker — shared by Account and Admin listing forms.
 * Keeps every selected file (append across dialogs), first file is primary.
 */
(function (window) {
    'use strict';

    function fileKey(file) {
        return [file.name, file.size, file.lastModified, file.type || ''].join('|');
    }

    function bindVehiclePhotosPicker(options) {
        options = options || {};
        var photoInput = document.getElementById(options.inputId || 'vehicle-photos');
        var previewRoot = document.getElementById(options.previewId || 'vehicle-photos-preview');
        var nameEl = document.getElementById(options.nameId || 'vehicle-photos-file-name');
        var urlInput = document.getElementById(options.urlId || 'Car_ImageUrl');
        var selectedFiles = [];
        var syncing = false;

        if (!photoInput || !previewRoot) {
            return null;
        }

        photoInput.multiple = true;
        if (!photoInput.hasAttribute('multiple')) {
            photoInput.setAttribute('multiple', 'multiple');
        }

        function syncInput() {
            syncing = true;
            try {
                var transfer = new DataTransfer();
                for (var i = 0; i < selectedFiles.length; i++) {
                    transfer.items.add(selectedFiles[i]);
                }
                photoInput.files = transfer.files;
            } finally {
                syncing = false;
            }
        }

        function renderSelected() {
            previewRoot.innerHTML = '';
            previewRoot.classList.add('av-vehicle-photos-grid');
            previewRoot.setAttribute('data-vehicle-photos-count', String(selectedFiles.length));

            if (nameEl) {
                nameEl.textContent = selectedFiles.length
                    ? ('Selected: ' + selectedFiles.map(function (file) { return file.name; }).join(', ')
                        + ' — first photo is primary')
                    : '';
            }

            for (var index = 0; index < selectedFiles.length; index++) {
                (function (file, fileIndex) {
                    var card = document.createElement('div');
                    card.className = 'av-vehicle-photo-card vehicle-photo-new';
                    card.setAttribute('data-preview-index', String(fileIndex));
                    card.setAttribute('data-file-name', file.name);

                    var media = document.createElement('div');
                    media.className = 'av-vehicle-photo-card__media';

                    if (fileIndex === 0) {
                        var badge = document.createElement('span');
                        badge.className = 'badge-primary-photo';
                        badge.textContent = 'Primary';
                        media.appendChild(badge);
                    }

                    if (!file.type || file.type.indexOf('image/') === 0) {
                        var image = document.createElement('img');
                        image.alt = file.name;
                        image.className = 'av-vehicle-photo-card__thumb';
                        image.src = URL.createObjectURL(file);
                        media.appendChild(image);
                    }

                    var remove = document.createElement('button');
                    remove.type = 'button';
                    remove.className = 'gallery-remove';
                    remove.title = 'Remove photo';
                    remove.setAttribute('aria-label', 'Remove photo');
                    remove.textContent = '\u00d7';
                    remove.addEventListener('click', function () {
                        selectedFiles.splice(fileIndex, 1);
                        syncInput();
                        renderSelected();
                    });
                    media.appendChild(remove);
                    card.appendChild(media);

                    var meta = document.createElement('div');
                    meta.className = 'av-vehicle-photo-card__meta';
                    if (fileIndex > 0) {
                        var makePrimary = document.createElement('button');
                        makePrimary.type = 'button';
                        makePrimary.className = 'btn btn-sm btn-outline-primary w-100';
                        makePrimary.textContent = 'Set as primary';
                        makePrimary.addEventListener('click', function () {
                            var moved = selectedFiles.splice(fileIndex, 1)[0];
                            selectedFiles.unshift(moved);
                            syncInput();
                            renderSelected();
                        });
                        meta.appendChild(makePrimary);
                    } else {
                        var label = document.createElement('div');
                        label.className = 'av-vehicle-photo-card__primary-label';
                        label.textContent = 'Primary photo';
                        meta.appendChild(label);
                    }
                    card.appendChild(meta);
                    previewRoot.appendChild(card);
                })(selectedFiles[index], index);
            }
        }

        function appendIncomingFiles(fileList) {
            if (!fileList || !fileList.length) {
                return;
            }
            var known = {};
            for (var k = 0; k < selectedFiles.length; k++) {
                known[fileKey(selectedFiles[k])] = true;
            }
            for (var i = 0; i < fileList.length; i++) {
                var file = fileList[i];
                if (!file) {
                    continue;
                }
                var key = fileKey(file);
                if (known[key]) {
                    continue;
                }
                known[key] = true;
                selectedFiles.push(file);
            }
            syncInput();
            renderSelected();
        }

        photoInput.addEventListener('change', function () {
            if (syncing) {
                return;
            }
            var incoming = photoInput.files;
            if (!incoming || incoming.length === 0) {
                return;
            }
            // Copy File references before clearing / resyncing the input.
            var copy = [];
            for (var i = 0; i < incoming.length; i++) {
                copy.push(incoming[i]);
            }
            appendIncomingFiles(copy);
        });

        document.querySelectorAll('.gallery-remove').forEach(function (button) {
            if (button.closest('#vehicle-photos-preview')) {
                return;
            }
            button.addEventListener('click', function () {
                var item = button.closest('.gallery-item');
                if (!item || !window.confirm('Remove this photo from the listing?')) {
                    return;
                }
                var id = item.getAttribute('data-image-id');
                var radio = item.querySelector('.gallery-primary');
                var wasPrimary = radio && radio.checked;
                if (radio) {
                    radio.checked = false;
                    radio.disabled = true;
                }
                var marker = document.createElement('input');
                marker.type = 'hidden';
                marker.name = 'Car.RemoveImageIds';
                marker.value = id;
                item.appendChild(marker);
                item.classList.add('d-none');
                if (wasPrimary) {
                    var next = document.querySelector('.gallery-item:not(.d-none) .gallery-primary');
                    if (next) {
                        next.checked = true;
                        if (urlInput) {
                            urlInput.value = next.getAttribute('data-url') || urlInput.value;
                        }
                    }
                }
            });
        });

        var listingForm = photoInput.closest('form') || document.querySelector('form.admin-card');
        if (listingForm) {
            listingForm.addEventListener('submit', function () {
                syncInput();
                if (selectedFiles.length > 0) {
                    return;
                }
                var selectedPrimary = document.querySelector('.gallery-primary:checked');
                if (selectedPrimary && urlInput) {
                    var typed = (urlInput.value || '').trim();
                    var selectedUrl = selectedPrimary.getAttribute('data-url') || '';
                    var known = Array.prototype.some.call(document.querySelectorAll('.gallery-primary'), function (radio) {
                        return (radio.getAttribute('data-url') || '').trim() === typed;
                    });
                    if (!typed || known) {
                        urlInput.value = selectedUrl;
                    }
                }
            });
        }

        var api = {
            getFiles: function () { return selectedFiles.slice(); },
            getCount: function () { return selectedFiles.length; },
            appendFiles: appendIncomingFiles,
            clear: function () {
                selectedFiles = [];
                syncInput();
                renderSelected();
            },
            render: renderSelected
        };
        window.__avVehiclePhotosPicker = api;
        return api;
    }

    window.AutoVaultVehiclePhotos = {
        bind: bindVehiclePhotosPicker
    };
})(window);
