$(document).ready(function () {
    var countryCode = ($('#countryCode').val() || '').toUpperCase().trim();
    var isdCode = ($('#Isd').val() || '').trim();

    // Detect India vs Australia
    var isIndia = (countryCode === 'IN' || isdCode === '91');
    var isAustralia = (countryCode === 'AU' || isdCode === '61');

    const $ifscLabel = $('label[for="IFSCCode"], .input-group-label:contains("IFSC Code")');
    var $ifscInput = $('#IFSCCode');

    function validateBankCode() {
        
        if (!$ifscInput.length) return;

        var code = ($ifscInput.val() || '').toUpperCase().trim();
        $ifscInput.val(code);

        // Reset UI
        $('#ifsc-valid-icon').hide();
        $('#ifsc-spinner').addClass('d-none');
        $('#BankName').val('').removeClass('invalid-border valid-border').removeAttr('data-bs-original-title data-original-title title');
        $ifscInput.removeClass('is-valid is-invalid').removeAttr('data-bs-original-title data-original-title title');

        if (isIndia) {
            // ------------------------ 🇮🇳 IFSC CHECK ------------------------
            var ifscRegex = /^[A-Z]{4}0[A-Z0-9]{6}$/;

            if (code.length === 11 && ifscRegex.test(code)) {
                $('#ifsc-spinner').removeClass('d-none');

                $.ajax({
                    url: 'https://ifsc.razorpay.com/' + encodeURIComponent(code),
                    method: 'GET',
                    success: function (data) {
                        $('#ifsc-spinner').addClass('d-none');

                        if (data && data.BANK) {
                            $('#BankName')
                                .val(data.BANK)
                                .addClass('valid-border')
                                .removeClass('invalid-border')
                                .attr('data-original-title', '🏦 ' + data.BANK + ', ' + data.BRANCH + ', ' + data.ADDRESS)
                                .attr('data-bs-original-title', '🏦 ' + data.BANK + ', ' + data.BRANCH + ', ' + data.ADDRESS);

                            $('#ifsc-valid-icon').show();
                            $ifscInput
                                .attr('data-original-title', '✅ Valid IFSC (' + data.BANK + ')')
                                .attr('data-bs-original-title', '✅ Valid IFSC (' + data.BANK + ')');
                        } else {
                            setInvalid('❌ Invalid IFSC Code');
                        }
                    },
                    error: function () {
                        setInvalid('❌ Unable to verify IFSC (API error)');
                    }
                });
            } else {
                setInvalid('❌ IFSC must be 11 characters and match format (e.g., SBIN0000001)');
            }
        }
        else if (isAustralia) {
            $ifscLabel.text('BSB Code:');
            $ifscInput
                .attr('placeholder', 'Enter 6-digit BSB code')
                .attr('maxlength', '6')
                .attr('data-original-title', 'Enter valid BSB code')
                .attr('data-bs-original-title', 'Enter valid BSB code');
            // ------------------------ 🇦🇺 BSB CHECK ------------------------
            var bsbRegex = /^\d{6}$/;
            if (bsbRegex.test(code)) {
                $('#ifsc-spinner').removeClass('d-none');
                
                $.ajax({
                    url: '/api/MasterData/bsb?code=' + encodeURIComponent(code),
                    method: 'GET',
                    success: function (data) {
                        $('#ifsc-spinner').addClass('d-none');
                        if (data && data.bank) {

                        var bankData = '🏦 ' + data.bank + ', ' + data.address + ', ' + data.city + ', ' + data.state + ', ' + data.postcode;
                        var branchData = '🏦 Branch: ' + data.branch + ', ' + data.address + ', ' + data.city + ', ' + data.state + ', ' + data.postcode;
                            $('#BankName')
                                .val(data.bank)
                                .addClass('valid-border')
                                .removeClass('invalid-border')
                                .attr('data-original-title', bankData)
                                .attr('data-bs-original-title', bankData);

                            $('#ifsc-valid-icon').show();
                            $ifscInput.addClass('is-valid')
                                .attr('data-original-title', '✅ Valid BSB')
                                .attr('data-bs-original-title', '✅ Valid BSB');
                        } else {
                            setInvalid('❌ Invalid BSB Code');
                        }
                    },
                    error: function (er) {
                        console.log(er);
                        setInvalid('❌ Unable to verify BSB (API error)');
                    }
                });
            } else {
                setInvalid('❌ BSB must be exactly 6 digits');
            }
        }
        else {
            // ------------------------ 🌍 Other countries ------------------------
            setInvalid('❌ Bank code validation available only for India (IFSC) and Australia (BSB)');
        }

        function setInvalid(msg) {
            $('#ifsc-spinner').addClass('d-none');
            $ifscInput.addClass('is-invalid').removeClass('valid-border').attr('data-bs-original-title', msg).attr('data-original-title', msg);
            $('#BankName')
                .val('')
                .addClass('invalid-border')
                .removeClass('valid-border')
                .attr('data-bs-original-title', msg)
                .attr('data-original-title', msg);
        }
    }

    function setLabels() {
        if (isAustralia) {
            $ifscLabel.text('BSB Code:');
            $ifscInput
                .attr('placeholder', 'Enter 6-digit BSB code')
                .attr('maxlength', '6')
                .attr('title', 'Enter valid BSB code');
        }
    }

    // Attach blur handler
    $(document).on('blur', '#IFSCCode', function () {
        validateBankCode();
    });

    // Run once on page load if editing
    $(window).on('load', function () {
        var existing = ($('#IFSCCode').val() || '').trim();
        if (existing.length > 0) {
            validateBankCode();
        }
        else {
            setLabels();
        }
    });

    $('input.auto-dropdown, #IFSCCode').on('focus', function () {
        $(this).select();
    });
});

document.addEventListener("DOMContentLoaded", function () {
    const phoneInput = document.getElementById("PhoneNumber");
    const validIcon = document.getElementById("phone-valid");
    const invalidIcon = document.getElementById("phone-invalid");
    const spinnerIcon = document.getElementById("phone-spinner");
    var countryCode = ($('#countryCode').val() || '').toUpperCase().trim();

    phoneInput.addEventListener('focus', function () {
        this.select();
    });

    let typingTimer;
    const typingDelay = 800; // milliseconds delay after typing stops

    ["input","blur"].forEach(evt => {
        phoneInput.addEventListener(evt, () => {
            clearTimeout(typingTimer);
            typingTimer = setTimeout(validatePhone, evt === "blur" ? 0 : typingDelay);
        });
    });

    function setTooltip(el, text) {
        // Remove old cached tooltip
        el.removeAttribute("data-bs-original-title");
        el.removeAttribute("data-original-title");
        el.removeAttribute('data-bs-original-title data-original-title title');
        el.setAttribute("data-bs-original-title", '');
        el.setAttribute("data-original-title", '');

        // Set new title
        el.setAttribute("data-bs-original-title", text);
        el.setAttribute("data-original-title", text);
    }
    async function validatePhone() {
        const phone = phoneInput.value.trim();
        const isd = document.getElementById("Isd").value.trim();
        if (phone.length == 0) {
            toggleSubmitButton(false);
            return;
        }
        showSpinner();
        try {
            const response = await fetch(`/api/MasterData/IsValidMobileNumber?phone=${encodeURIComponent(phone)}&countryCode=${isd}`);
            const data = await response.json();

            if (data.valid) {
                setTooltip(phoneInput, `✅ Valid ${countryCode} (📱) mobile #`);
                //phoneInput.title = `✅ Valid ${countryCode} (📱) mobile #`;
                validIcon.classList.remove("d-none");
                invalidIcon.classList.add("d-none");
                phoneInput.classList.remove("is-invalid");
                phoneInput.classList.add("is-valid");
                toggleSubmitButton(true);
            } else {
                setTooltip(phoneInput, `❌ Invalid ${countryCode} (📱) mobile #`);

                //phoneInput.title = `❌ Invalid ${countryCode} (📱) mobile #`;
                invalidIcon.classList.remove("d-none");
                validIcon.classList.add("d-none");
                phoneInput.classList.remove("is-valid");
                phoneInput.classList.add("is-invalid");
                toggleSubmitButton(false);
            }
        } catch (err) {
            console.error(" (📱) Mobile # validation failed:", err);
            invalidIcon.classList.remove("d-none");
            validIcon.classList.add("d-none");
            phoneInput.classList.remove("is-valid");
            phoneInput.classList.add("is-invalid");
            setTooltip(phoneInput, "❌ Mobile # validation error");

            //phoneInput.title = "❌ Mobile # validation error";
            toggleSubmitButton(false);
        } finally {
            hideSpinner();
        }
    }
    function showSpinner() {
        spinnerIcon.classList.remove("d-none");
        validIcon.classList.add("d-none");
        invalidIcon.classList.add("d-none");
        phoneInput.classList.remove("is-valid", "is-invalid");
    }
    function hideSpinner() {
        spinnerIcon.classList.add("d-none");
    }

    validatePhone();

});

function toggleSubmitButton(isValid) {
    const form = document.getElementById("create-form") || document.getElementById("edit-form");
    const submitButton = document.getElementById("create") || document.getElementById("edit");
    const inputs = form.querySelectorAll(".remarks[required], .remarks[aria-required='true']");
    let allValid = [...inputs].every(i => i.value.trim() && !i.classList.contains("is-invalid"));
    const emailAddress = document.getElementById("emailAddress");
    if (emailAddress) {
        const emailData = emailAddress.value;
        if (!emailData) {
            allValid = false;
        }
    }
    submitButton.disabled = !allValid;
}