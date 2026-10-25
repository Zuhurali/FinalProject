/**
 * AutoVault contact form — AJAX submit with JSON responses only.
 * Never renders raw JSON, HTML, or responseText into #form-messages.
 **/
(function ($) {
    'use strict';

    var PHONE_ERROR = 'Enter a valid phone number.';
    var GENERIC_ERROR = 'Something went wrong. Please try again or use the contact page.';
    var VALIDATION_SUMMARY = 'Please correct the highlighted fields.';
    var SUCCESS_FALLBACK = 'Thank you. Your message was sent.';
    var MIN_PHONE_DIGITS = 7;
    var MAX_PHONE_DIGITS = 15;
    var MAX_PHONE_LENGTH = 25;

    function looksLikeHtml(value) {
        if (typeof value !== 'string') {
            return false;
        }
        var trimmed = value.trim().slice(0, 200).toLowerCase();
        return trimmed.indexOf('<!doctype') === 0
            || trimmed.indexOf('<html') === 0
            || trimmed.indexOf('<head') !== -1
            || trimmed.indexOf('<body') !== -1;
    }

    function looksLikeJson(value) {
        if (typeof value !== 'string') {
            return false;
        }
        var trimmed = value.trim();
        if (!trimmed) {
            return false;
        }
        var first = trimmed.charAt(0);
        return first === '{' || first === '[';
    }

    function isPlainObject(value) {
        return !!value && typeof value === 'object' && !Array.isArray(value);
    }

    /** Only human-readable summary text — never JSON/HTML/response bodies. */
    function safeMessage(value, fallback) {
        if (typeof value !== 'string' || !value.trim() || looksLikeHtml(value) || looksLikeJson(value)) {
            return fallback || GENERIC_ERROR;
        }
        return value.trim();
    }

    function isValidPhone(value) {
        if (typeof value !== 'string') {
            return false;
        }
        var phone = value.trim();
        if (!phone || phone.length > MAX_PHONE_LENGTH) {
            return false;
        }
        for (var i = 0; i < phone.length; i++) {
            var ch = phone.charAt(i);
            if (!(ch >= '0' && ch <= '9') && ch !== '+' && ch !== ' ' && ch !== '-' && ch !== '(' && ch !== ')') {
                return false;
            }
        }
        if (phone.indexOf('+') !== -1 && (phone.charAt(0) !== '+' || phone.split('+').length > 2)) {
            return false;
        }
        var digits = phone.replace(/\D/g, '');
        if (digits.length < MIN_PHONE_DIGITS || digits.length > MAX_PHONE_DIGITS) {
            return false;
        }
        return /^\+?(?:[\s\-()]*\d){7,15}[\s\-()]*$/.test(phone);
    }

    function clearFieldErrors($form) {
        $form.find('.av-field-error').text('').attr('hidden', true);
        $form.find('.input-form, textarea').removeClass('is-invalid');
    }

    function showFieldError($form, field, message) {
        var text = safeMessage(message, '');
        if (!text) {
            return;
        }
        var $input = $form.find('[name="' + field + '"]');
        var $error = $form.find('.av-field-error[data-field="' + field + '"]');
        if ($input.length) {
            $input.addClass('is-invalid');
        }
        if ($error.length) {
            $error.text(text).removeAttr('hidden');
        }
    }

    function normalizeErrors(errors) {
        var normalized = {};
        if (!isPlainObject(errors)) {
            return normalized;
        }
        Object.keys(errors).forEach(function (key) {
            var value = errors[key];
            if (typeof value === 'string' && value.trim() && !looksLikeJson(value) && !looksLikeHtml(value)) {
                normalized[String(key).toLowerCase()] = value.trim();
            }
        });
        return normalized;
    }

    /**
     * Accepts a parsed object or a raw response string. Never returns raw bodies for display.
     * @returns {{ success: boolean, message: string, errors: Object }|null}
     */
    function parseJsonPayload(data) {
        var parsed = data;

        if (typeof data === 'string') {
            if (!data.trim() || looksLikeHtml(data)) {
                return null;
            }
            try {
                parsed = JSON.parse(data);
            } catch (e) {
                return null;
            }
        }

        if (!isPlainObject(parsed)) {
            return null;
        }

        var errors = normalizeErrors(parsed.errors);
        var hasErrors = Object.keys(errors).length > 0;
        var success = parsed.success === true;
        var message = typeof parsed.message === 'string' ? parsed.message : '';

        return {
            success: success,
            message: safeMessage(
                message,
                success ? SUCCESS_FALLBACK : (hasErrors ? VALIDATION_SUMMARY : GENERIC_ERROR)
            ),
            errors: errors
        };
    }

    function applyErrors($form, $messages, payload) {
        var errors = payload && payload.errors ? payload.errors : {};
        var summary = payload && payload.message
            ? payload.message
            : (Object.keys(errors).length ? VALIDATION_SUMMARY : GENERIC_ERROR);

        clearFieldErrors($form);
        $messages.removeClass('success').addClass('error');
        Object.keys(errors).forEach(function (key) {
            showFieldError($form, key, errors[key]);
        });
        $messages.text(safeMessage(summary, VALIDATION_SUMMARY));
    }

    function applySuccess($form, $messages, payload) {
        $messages.removeClass('error').addClass('success');
        $messages.text(safeMessage(payload && payload.message, SUCCESS_FALLBACK));
        $form.find('#name, #email, #phone, #car, #message').val('');
        clearFieldErrors($form);
    }

    function showGenericError($messages) {
        $messages.removeClass('success').addClass('error');
        $messages.text(GENERIC_ERROR);
    }

    function clientValidate($form) {
        var errors = {};
        var name = ($form.find('[name="name"]').val() || '').trim();
        var email = ($form.find('[name="email"]').val() || '').trim();
        var phone = ($form.find('[name="phone"]').val() || '').trim();
        var car = ($form.find('[name="car"]').val() || '').trim();
        var message = ($form.find('[name="message"]').val() || '').trim();

        if (name.length < 2) {
            errors.name = 'Enter your name.';
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            errors.email = 'Enter a valid email address.';
        }
        if (!isValidPhone(phone)) {
            errors.phone = PHONE_ERROR;
        }
        if (car.length < 2) {
            errors.car = 'Tell us which part or car you are interested in.';
        }
        if (message.length < 10) {
            errors.message = 'Write at least 10 characters in your message.';
        }

        return errors;
    }

    function handlePayload($form, $messages, payload) {
        if (!payload) {
            showGenericError($messages);
            return;
        }
        if (payload.success === true) {
            applySuccess($form, $messages, payload);
            var redirect = $form.attr('data-redirect-on-success');
            if (redirect) {
                window.setTimeout(function () {
                    window.location.href = redirect;
                }, 900);
            }
            return;
        }
        applyErrors($form, $messages, payload);
    }

    $(function () {
        var $forms = $('form.av-contact-form');
        if (!$forms.length) {
            return;
        }

        $forms.each(function () {
            var $form = $(this);
            var $messages = $form.siblings('#form-messages');
            if (!$messages.length) {
                $messages = $form.closest('.contact-form').find('#form-messages').first();
            }

            // Avoid duplicate handlers if the script is included more than once.
            $form.off('submit.avContact');
            $form.find('[name="phone"]').off('.avContact');

            $form.on('submit.avContact', function (e) {
                e.preventDefault();
                clearFieldErrors($form);
                $messages.removeClass('success error').text('');

                var clientErrors = clientValidate($form);
                if (Object.keys(clientErrors).length) {
                    applyErrors($form, $messages, {
                        success: false,
                        message: VALIDATION_SUMMARY,
                        errors: clientErrors
                    });
                    return;
                }

                var $submit = $form.find('[type="submit"]');
                $submit.prop('disabled', true);

                $.ajax({
                    type: 'POST',
                    url: $form.attr('action'),
                    data: $form.serialize(),
                    // Parse manually so 400 JSON bodies are never dumped into the UI.
                    dataType: 'text',
                    headers: {
                        'Accept': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                })
                    .done(function (text) {
                        handlePayload($form, $messages, parseJsonPayload(text));
                    })
                    .fail(function (xhr) {
                        var raw = xhr && typeof xhr.responseText === 'string' ? xhr.responseText : '';
                        var payload = parseJsonPayload(raw);
                        if (payload) {
                            handlePayload($form, $messages, payload);
                            return;
                        }
                        showGenericError($messages);
                    })
                    .always(function () {
                        $submit.prop('disabled', false);
                    });
            });

            $form.find('[name="phone"]').on('input.avContact blur.avContact', function () {
                var $phone = $(this);
                var value = ($phone.val() || '').trim();
                var $error = $form.find('.av-field-error[data-field="phone"]');
                if (!value) {
                    $phone.removeClass('is-invalid');
                    $error.text('').attr('hidden', true);
                    return;
                }
                if (!isValidPhone(value)) {
                    $phone.addClass('is-invalid');
                    $error.text(PHONE_ERROR).removeAttr('hidden');
                } else {
                    $phone.removeClass('is-invalid');
                    $error.text('').attr('hidden', true);
                }
            });
        });
    });
})(jQuery);
