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
                            $ifscInput.addClass('is-valid')
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
                    url: '/api/company/bsb?code=' + encodeURIComponent(code),
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

    const fields = ['#CountryId', '#StateText', '#DistrictText', '#PinCodeText'];
    $("#StateText").prop("disabled", true).val("");
    $("#PinCodeText").prop("disabled", true);
    fields.forEach(function (field) {
        $(field).on('input', function () {
            const isValid = /^[a-zA-Z0-9 ]*$/.test(this.value); // Check if input is valid
            if (!isValid) {
                $(this).val('');
            }
        });
    });

    $('input.auto-dropdown, #IFSCCode').on('focus', function () {
        $(this).select();
    });

    const preloadedCountryId = $("#SelectedCountryId").val();
    const preloadedPincodeId = $("#SelectedPincodeId").val();

    if (preloadedCountryId) {
        // Preload country name
        fetchAndSetFieldValue("/api/Company/GetCountryName", { id: preloadedCountryId }, "#CountryId", "name");

        $("#StateText").prop("disabled", false);
        $("#PinCodeText").prop("disabled", false);

        // Preload details if Pincode is provided
        if (preloadedPincodeId) {
            preloadPincodeDetails(preloadedCountryId, preloadedPincodeId);
        }
    }
    $("#CountryId").on("input change", function () {
        const selectedCountryId = $("#SelectedCountryId").val();

        if (selectedCountryId) {
            stateAutocomplete();
            $("#StateText").prop("disabled", false);
            $("#DistrictText").prop("disabled", false);
            $("#PinCodeText").prop("disabled", false);
        } else {
            $("#StateText").prop("disabled", true);
            $("#PinCodeText").prop("disabled", true);

            // Reset dependent fields
            resetField("#PinCodeText");
            resetField("#StateText");
            resetField("#DistrictText");
        }
    });

    $("#StateText").on("blur", function () {
        const stateIdValue = $(this).val().trim();

        if (!stateIdValue) {
            $("#PinCodeText").prop("disabled", true);
            resetField("#DistrictText");
            resetField("#PinCodeText");
        }
        else {
            districtAutocomplete();
            $("#StateText").prop("disabled", false);
            $("#DistrictText").prop("disabled", false);
            $("#PinCodeText").prop("disabled", false);
        }
    });

    // Watch for changes in CountryId
    $("#StateText").on("input change", function () {
        const selectedStateId = $("#SelectedStateId").val();

        if (selectedStateId) {
            $("#DistrictText").prop("disabled", false);
            $("#PinCodeText").prop("disabled", false);
            districtAutocomplete();

        } else {
            $("#PinCodeText").prop("disabled", true);

            // Reset dependent fields
            resetField("#PinCodeText");
            resetField("#DistrictText");
        }
    });

    $("#StateText").on("blur", function () {
        const stateValue = $(this).val().trim();
        const countryId = $("#SelectedCountryId").val();
        validateStateSelection(stateValue, countryId);
    });

    $("#DistrictText").on("blur", function () {
        const districtValue = $(this).val().trim();
        const stateId = $("#SelectedStateId").val();
        const countryId = $("#SelectedCountryId").val();
        validateDistrictSelection(districtValue, stateId, countryId);
    });

    // Initialize country autocomplete
    countryAutocomplete();

    //Initialize state autocomplete
    stateAutocomplete();

    //Initialize district autocomplete
    districtAutocomplete();

    // Initialize autocomplete for Pincode
    pincodeAutocomplete();
});
function preloadPincodeDetails(preloadedCountryId, preloadedPincodeId) {
    showLoader("#pincode-loading");

    $.ajax({
        url: "/api/Company/GetPincode",
        type: "GET",
        data: { id: preloadedPincodeId, countryId: preloadedCountryId },
        success: function (response) {
            hideLoader("#pincode-loading");

            if (response) {
                // Preload Pincode
                $("#PinCodeText").val(response.pincodeName);
                $("#SelectedPincodeId").val(response.pincodeId);

                // Preload State
                if (response.stateId) {
                    fetchAndSetFieldValue(
                        "/api/Company/GetStateName",
                        { id: response.stateId, CountryId: preloadedCountryId },
                        "#StateText",
                        "stateName"
                    );
                }

                // Preload District
                if (response.districtId) {
                    fetchAndSetFieldValue(
                        "/api/Company/GetDistrictName",
                        { id: response.districtId, stateId: response.stateId, CountryId: preloadedCountryId },
                        "#DistrictText",
                        "districtName"
                    );
                }
            } else {
                $("#PinCodeText").addClass("invalid")
                resetField("#PinCodeText");
                resetField("#StateText");
                resetField("#DistrictText");
            }
        },
        error: function () {
            hideLoader("#pincode-loading");
            $("#PinCodeText").addClass("invalid")
        }
    });
}

function countryAutocomplete() {
    $("#CountryId").autocomplete({
        source: function (request, response) {
            fetchCountrySuggestions(request.term, function (suggestions) {
                // Filter out non-selectable items
                response(suggestions);
            });
        },
        focus: function (event, ui) {
            if (ui.item.isSelectable === false) {
                $("#CountryId").val('');
                $("#CountryId").addClass("invalid");
                return false;
            }
            // Set the input field to the "label" value when navigating with arrow keys
            $("#CountryId").val(ui.item.label);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            if (ui.item.isSelectable === false) {
                // Prevent selection if it's the "No result found"
                $("#CountryId").addClass("invalid");
                return false;
            } else {
                $("#CountryId").removeClass("invalid");
                // On selecting a country, populate the field with its name and set its ID
                $("#CountryId").val(ui.item.label); // Set country name
                $("#SelectedCountryId").val(ui.item.value); // Set hidden field for CountryId
                $("#PinCodeText").prop("disabled", false).attr("placeholder", "Search Pincode or name ...");

                // Reset dependent fields (State, District, Pincode) when the country changes
                resetField("#StateText");
                resetField("#SelectedStateId");
                resetField("#DistrictText");
                resetField("#SelectedDistrictId");
                resetField("#PinCodeText");
                resetField("#SelectedPincodeId");

                return false; // Prevent default autocomplete behavior
            }
        },
        minLength: 2 // Minimum number of characters to trigger suggestions
    });
    $("#CountryId").on("blur", function () {
        validateCountrySelection($(this).val(), $("#SelectedCountryId").val());
    });
}
function fetchCountrySuggestions(term, responseCallback) {
    $.ajax({
        url: "/api/Company/GetCountrySuggestions", // API endpoint for country suggestions
        type: "GET",
        data: { term: term },
        success: function (data) {
            const suggestions = data.map(item => ({
                label: `${item.name}`,
                value: item.id,
                name: item.name,
            }));
            // If no suggestions found, add the "No result found" option
            if (suggestions.length === 0) {
                suggestions.push({
                    label: "No result found",
                    value: "",
                    name: null,
                    isSelectable: false // Mark it as non-selectable
                });
            }
            responseCallback(suggestions);
        },
        error: function () {
            console.log("Error fetching Country suggestions.");
        }
    });
}

function validateCountrySelection(inputValue, countryId) {
    if (!inputValue) {
        $("#CountryId").val("");
        $("#SelectedCountryId").val("");
        markInvalidField("#CountryId");
        resetField("#StateText");
        resetField("#SelectedStateId");
        resetField("#DistrictText");
        resetField("#SelectedDistrictId");
        resetField("#PinCodeText");
        resetField("#SelectedPincodeId");
        return;
    }

    $.ajax({
        url: "/api/Company/GetCountrySuggestions",
        type: "GET",
        data: { term: inputValue},
        success: function (data) {
            const isValid = data.some(item =>
                `${item.name}` === inputValue);

            if (!isValid) {
                markInvalidField("#CountryId");
                $("#CountryId").val("");
                $("#SelectedCountryId").val("");
                $("#CountryId").focus();
            } else {
                $("#PinCodeText").prop("disabled", false).attr("placeholder", "Search Pincode or name ...");
                $("#PinCodeText").focus();
                $("#CountryId").removeClass("invalid"); // Remove invalid class if valid
            }
        },
        error: function () {
            console.log("Error validating Country.");
            markInvalidField("#CountryId");
            $("#CountryId").val("");
            $("#SelectedCountryId").val("");
        }
    });
}

function pincodeAutocomplete() {
    const pinCodeField = "#PinCodeText";
    const selectedPinCodeField = "#SelectedPincodeId";
    const selectedCountryField = "#SelectedCountryId";

    // Initialize autocomplete
    $(pinCodeField).autocomplete({
        source: function (request, response) {
            fetchPincodeSuggestions(request.term, $(selectedCountryField).val(), function (suggestions) {
                // Filter out non-selectable items
                response(suggestions);
            });
        },
        focus: function (event, ui) {
            if (ui.item.isSelectable === false) {
                $(pinCodeField).val('');
                $(pinCodeField).addClass("invalid");
                return false;
            }
            // Set the input field to the "label" value when navigating with arrow keys
            $(pinCodeField).val(ui.item.label);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            if (ui.item.isSelectable === false) {
                // Prevent selection if it's the "No result found"
                $(pinCodeField).addClass("invalid");
                return false;
            }
            populatePincodeDetails(ui.item);
            $(pinCodeField).removeClass("invalid");
            return false;
        },
        minLength: 2
    });

    // Validate on blur to ensure a valid option is selected
    $(pinCodeField).on("blur", function () {
        validatePincodeSelection($(this).val(), $(selectedCountryField).val());
    });
}

function fetchPincodeSuggestions(term, countryId, responseCallback) {
    $.ajax({
        url: "/api/Company/GetPincodeSuggestions",
        type: "GET",
        data: { term: term, countryId: countryId },
        success: function (data) {
            const suggestions = data.map(item => ({
                label: `${item.name} - ${item.pincode}`,
                value: item.pincodeId,
                stateId: item.stateId,
                stateName: item.stateName,
                districtId: item.districtId,
                districtName: item.districtName
            }));
            // If no suggestions found, add the "No result found" option
            if (suggestions.length === 0) {
                suggestions.push({
                    label: "No result found",
                    value: "",
                    stateId: null,
                    stateName: null,
                    districtId: null,
                    districtName: null,
                    isSelectable: false // Mark it as non-selectable
                });
            }
            responseCallback(suggestions);
        },
        error: function () {
            console.log("Error fetching Pincode suggestions.");
        }
    });
}

function populatePincodeDetails(selectedItem) {
    $("#PinCodeText").val(selectedItem.label);
    $("#SelectedPincodeId").val(selectedItem.value);
    $("#StateText").removeClass("invalid");
    $("#StateText").addClass("valid-border");
    $("#StateText").val(selectedItem.stateName);
    $("#SelectedStateId").val(selectedItem.stateId);
    $("#DistrictText").removeClass("invalid");
    $("#DistrictText").addClass("valid-border");
    $("#DistrictText").val(selectedItem.districtName);
    $("#SelectedDistrictId").val(selectedItem.districtId);
    $("#PinCodeText").removeClass("invalid");
}

function validatePincodeSelection(inputValue, countryId) {
    if (!inputValue) {
        clearPincodeFields();
        markInvalidField("#CountryId");
        return;
    }

    $.ajax({
        url: "/api/Company/GetPincodeSuggestions",
        type: "GET",
        data: { term: inputValue, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item =>
                `${item.name} - ${item.pincode}` === inputValue);

            if (!isValid) {
                markInvalidField("#CountryId");
                clearPincodeFields();
                //alert("Please select a valid Pincode from the dropdown.");
            } else {
                var countryIdVisible = $("#CountryId");
                if (countryIdVisible) {
                    $("#CountryId").removeClass("invalid"); // Remove invalid class if valid
                }
            }
        },
        error: function () {
            console.log("Error validating Pincode.");
            markInvalidField("#CountryId");
            clearPincodeFields();
        }
    });
}

function validateStateSelection(inputValue, countryId) {
    if (!inputValue) {
        clearStateFields();
        markInvalidField("#StateText");
        return;
    }

    $.ajax({
        url: "/api/Company/SearchState",
        type: "GET",
        data: { term: inputValue, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item => item.stateName === inputValue);

            if (!isValid) {
                markInvalidField("#StateText");
                clearStateFields();
            } else {
                $("#StateText").removeClass("invalid");
            }
        },
        error: function () {
            markInvalidField("#StateText");
            clearStateFields();
        }
    });
}

function clearStateFields() {
    $("#StateText").val("");
    $("#SelectedStateId").val("");
    $("#DistrictText").val("");
    $("#SelectedDistrictId").val("");
    $("#PinCodeText").val("");
    $("#SelectedPincodeId").val("");
}

function validateDistrictSelection(inputValue, stateId, countryId) {
    if (!inputValue) {
        clearDistrictFields();
        markInvalidField("#DistrictText");
        return;
    }

    $.ajax({
        url: "/api/Company/SearchDistrict",
        type: "GET",
        data: { term: inputValue, stateId: stateId, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item => item.districtName === inputValue);

            if (!isValid) {
                markInvalidField("#DistrictText");
                clearDistrictFields();
            } else {
                $("#DistrictText").removeClass("invalid");
            }
        },
        error: function () {
            markInvalidField("#DistrictText");
            clearDistrictFields();
        }
    });
}

function clearDistrictFields() {
    $("#DistrictText").val("");
    $("#SelectedDistrictId").val("");
    $("#PinCodeText").val("");
    $("#SelectedPincodeId").val("");
}

function markInvalidField(fieldSelector) {
    $(fieldSelector).addClass("invalid");
}

function clearPincodeFields() {
    $("#PinCodeText").val("");
    $("#SelectedPincodeId").val("");
    $("#StateText").val("");
    $("#SelectedStateId").val("");
    $("#DistrictText").val("");
    $("#SelectedDistrictId").val("");
}

function stateAutocomplete() {
    $("#StateText").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/api/Company/SearchState",
                type: "GET",
                data: { term: request.term, countryId: $("#SelectedCountryId").val() },
                success: function (data) {
                    response(data.map(function (item) {
                        return {
                            label: `${item.stateName}`,
                            value: item.stateId,
                            stateName: item.stateName,
                        };
                    }));
                },
                error: function () {
                    console.log("Error fetching Pincode suggestions.");
                }
            });
        },
        focus: function (event, ui) {
            // Set the input field to the "label" value when navigating with arrow keys
            $("#StateText").val(ui.item.label);
            $("#SelectedStateId").val(ui.item.value);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            $("#StateText").val(ui.item.label);
            $("#SelectedStateId").val(ui.item.value);

            return false;
        },
        minLength: 2
    });
}

function districtAutocomplete() {
    $("#DistrictText").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/api/Company/SearchDistrict",
                type: "GET",
                data: { term: request.term, stateId: $("#SelectedStateId").val(), countryId: $("#SelectedCountryId").val() },
                success: function (data) {
                    response(data.map(function (item) {
                        return {
                            label: `${item.districtName}`,
                            value: item.districtId,
                            districtName: item.districtName,
                        };
                    }));
                },
                error: function () {
                    console.log("Error fetching district suggestions.");
                }
            });
        },
        focus: function (event, ui) {
            $("#DistrictText").val(ui.item.label);
            $("#SelectedDistrictId").val(ui.item.value);

            return false;
        },
        select: function (event, ui) {
            $("#DistrictText").val(ui.item.label);
            $("#SelectedDistrictId").val(ui.item.value);

            return false;
        },
        minLength: 2
    });
}

function fetchAndSetFieldValue(url, data, fieldSelector, fieldName, callback = null) {
    showLoader(`${fieldSelector}-loading`);

    $.ajax({
        url: url,
        type: "GET",
        data: data,
        success: function (response) {
            hideLoader(`${fieldSelector}-loading`);
            if (response && response[fieldName]) {
                $(fieldSelector).val(response[fieldName]);
                $(fieldSelector).addClass('valid-border');
            }
            if (callback) callback(response);
        },
        error: function () {
            hideLoader(`${fieldSelector}-loading`);
        }
    });
}

function resetField(fieldSelector) {
    $(fieldSelector).val("");
}

function showLoader(selector) {
    $(selector).show();
}

function hideLoader(selector) {
    $(selector).hide();
}

function markInvalidField(fieldSelector) {
    $(fieldSelector).addClass("invalid");
}

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
            const response = await fetch(`/api/Company/IsValidMobileNumber?phone=${encodeURIComponent(phone)}&countryCode=${isd}`);
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