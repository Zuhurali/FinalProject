/**
 * AutoVault car loan calculator — numeric guards + server-side calculation.
 */
(function () {
    'use strict';

    var form = document.getElementById('carLoanCalculator');
    if (!form) {
        return;
    }

    var notice = document.getElementById('loan-form-messages');
    var results = document.getElementById('loanResults');
    var submitBtn = document.getElementById('loanCalculateBtn');
    var integerInputs = form.querySelectorAll('[data-numeric="integer"]');
    var decimalInputs = form.querySelectorAll('[data-numeric="decimal"]');

    function isModifierShortcut(event) {
        return event.ctrlKey || event.metaKey || event.altKey;
    }

    function isNavigationKey(event) {
        var key = event.key;
        return key === 'Backspace'
            || key === 'Delete'
            || key === 'Tab'
            || key === 'Escape'
            || key === 'Enter'
            || key === 'ArrowLeft'
            || key === 'ArrowRight'
            || key === 'ArrowUp'
            || key === 'ArrowDown'
            || key === 'Home'
            || key === 'End';
    }

    function allowIntegerKey(event) {
        if (isModifierShortcut(event) || isNavigationKey(event)) {
            return;
        }
        if (event.key.length === 1 && !(event.key >= '0' && event.key <= '9')) {
            event.preventDefault();
        }
    }

    function allowDecimalKey(event) {
        if (isModifierShortcut(event) || isNavigationKey(event)) {
            return;
        }
        if (event.key === '.') {
            if (event.target.value.indexOf('.') !== -1) {
                event.preventDefault();
            }
            return;
        }
        if (event.key.length === 1 && !(event.key >= '0' && event.key <= '9')) {
            event.preventDefault();
        }
    }

    function sanitizeInteger(value) {
        return String(value || '').replace(/[^\d]/g, '');
    }

    function sanitizeDecimal(value) {
        var cleaned = String(value || '').replace(/[^\d.]/g, '');
        var parts = cleaned.split('.');
        if (parts.length === 1) {
            return parts[0];
        }
        return parts[0] + '.' + parts.slice(1).join('').replace(/\./g, '');
    }

    function bindNumericInput(input, mode) {
        input.addEventListener('keydown', mode === 'integer' ? allowIntegerKey : allowDecimalKey);

        input.addEventListener('paste', function (event) {
            event.preventDefault();
            var text = (event.clipboardData || window.clipboardData).getData('text');
            var sanitized = mode === 'integer' ? sanitizeInteger(text) : sanitizeDecimal(text);
            var start = input.selectionStart || 0;
            var end = input.selectionEnd || 0;
            var next = input.value.slice(0, start) + sanitized + input.value.slice(end);
            input.value = mode === 'integer' ? sanitizeInteger(next) : sanitizeDecimal(next);
            input.dispatchEvent(new Event('input', { bubbles: true }));
        });

        input.addEventListener('input', function () {
            var sanitized = mode === 'integer' ? sanitizeInteger(input.value) : sanitizeDecimal(input.value);
            if (input.value !== sanitized) {
                input.value = sanitized;
            }
        });
    }

    integerInputs.forEach(function (input) { bindNumericInput(input, 'integer'); });
    decimalInputs.forEach(function (input) { bindNumericInput(input, 'decimal'); });

    function clearErrors() {
        form.querySelectorAll('.av-loan-error').forEach(function (el) {
            el.textContent = '';
            el.hidden = true;
        });
        form.querySelectorAll('.av-loan-input, .nice-select').forEach(function (el) {
            el.classList.remove('is-invalid');
        });
        if (notice) {
            notice.hidden = true;
            notice.textContent = '';
            notice.classList.remove('is-error', 'is-success');
        }
    }

    function showNotice(message, isError) {
        if (!notice) {
            return;
        }
        notice.hidden = false;
        notice.textContent = message;
        notice.classList.toggle('is-error', !!isError);
        notice.classList.toggle('is-success', !isError);
    }

    function showFieldError(name, message) {
        var errorEl = form.querySelector('[data-error-for="' + name + '"]');
        var field = form.querySelector('[name="' + name + '"]');
        if (errorEl) {
            errorEl.textContent = message;
            errorEl.hidden = false;
        }
        if (field) {
            field.classList.add('is-invalid');
            var nice = field.closest('.av-loan-control') && field.closest('.av-loan-control').querySelector('.nice-select');
            if (nice) {
                nice.classList.add('is-invalid');
            }
        }
    }

    function clientValidate() {
        var errors = {};
        var carMake = (form.carMake && form.carMake.value || '').trim();
        var carPrice = (form.carPrice && form.carPrice.value || '').trim();
        var downPayment = (form.downPayment && form.downPayment.value || '').trim();
        var loanTermMonths = (form.loanTermMonths && form.loanTermMonths.value || '').trim();
        var annualInterestRate = (form.annualInterestRate && form.annualInterestRate.value || '').trim();

        if (!carMake) {
            errors.carMake = 'Select a car make.';
        }
        if (!carPrice) {
            errors.carPrice = 'Enter the car price.';
        } else if (!/^\d+(\.\d{1,2})?$/.test(carPrice) || Number(carPrice) <= 0) {
            errors.carPrice = 'Enter a valid car price using digits and an optional decimal point.';
        }
        if (!downPayment) {
            errors.downPayment = 'Enter the down payment.';
        } else if (!/^\d+(\.\d{1,2})?$/.test(downPayment)) {
            errors.downPayment = 'Enter a valid down payment using digits and an optional decimal point.';
        } else if (!errors.carPrice && Number(downPayment) > Number(carPrice)) {
            errors.downPayment = 'Down payment cannot be greater than the car price.';
        }
        if (!loanTermMonths) {
            errors.loanTermMonths = 'Enter the loan term in months.';
        } else if (!/^\d+$/.test(loanTermMonths) || Number(loanTermMonths) < 1) {
            errors.loanTermMonths = 'Loan term must be a whole number of months.';
        }
        if (!annualInterestRate) {
            errors.annualInterestRate = 'Enter the annual interest rate.';
        } else if (!/^\d+(\.\d{1,4})?$/.test(annualInterestRate)) {
            errors.annualInterestRate = 'Enter a valid annual interest rate using digits and an optional decimal point.';
        }

        return errors;
    }

    function formatMoney(value) {
        var amount = Number(value);
        if (!isFinite(amount)) {
            return '';
        }
        return '$' + amount.toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function setResult(id, value) {
        var el = document.getElementById(id);
        if (el) {
            el.value = formatMoney(value);
        }
    }

    function applyResult(result) {
        if (!results) {
            return;
        }
        results.hidden = false;
        setResult('loanPrincipal', result.principal);
        setResult('monthlyPayment', result.monthlyPayment);
        setResult('totalLoanPayments', result.totalLoanPayments);
        setResult('totalInterest', result.totalInterest);
        setResult('totalCost', result.totalCostIncludingDownPayment);
    }

    function clearResults() {
        if (!results) {
            return;
        }
        results.hidden = true;
        ['loanPrincipal', 'monthlyPayment', 'totalLoanPayments', 'totalInterest', 'totalCost'].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) {
                el.value = '';
            }
        });
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        clearErrors();
        clearResults();

        var localErrors = clientValidate();
        var keys = Object.keys(localErrors);
        if (keys.length) {
            keys.forEach(function (key) {
                showFieldError(key, localErrors[key]);
            });
            showNotice('Please correct the highlighted fields.', true);
            return;
        }

        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Calculating…';
        }

        var body = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: body,
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            credentials: 'same-origin'
        })
            .then(function (response) {
                return response.json().then(function (data) {
                    return { ok: response.ok, status: response.status, data: data };
                });
            })
            .then(function (payload) {
                var data = payload.data || {};
                if (!payload.ok || !data.success || !data.result) {
                    var errors = data.errors || {};
                    Object.keys(errors).forEach(function (key) {
                        showFieldError(key, errors[key]);
                    });
                    showNotice(data.message || 'Please correct the highlighted fields.', true);
                    return;
                }

                applyResult(data.result);
                showNotice(data.message || 'Loan estimate calculated.', false);
            })
            .catch(function () {
                showNotice('Something went wrong while calculating. Please try again.', true);
            })
            .finally(function () {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.textContent = 'Calculate';
                }
            });
    });
})();
