$(document).ready(function () {
    const fields = ['#CountryId', '#StateId', '#DistrictId', '#PinCodeId'];
    // Disable PinCodeId by default
    $("#StateId").prop("disabled", true).val("");
    $("#PinCodeId").prop("disabled", true);
    fields.forEach(function (field) {
        $(field).on('input', function () {
            const isValid = /^[a-zA-Z0-9 ]*$/.test(this.value); // Check if input is valid
            if (!isValid) {
                $(this).val('');
            }
        });
    });

    $('input.auto-dropdown').on('focus', function () {
        $(this).select();
    });

    const preloadedCountryId = $("#SelectedCountryId").val();
    const preloadedPincodeId = $("#SelectedPincodeId").val();// $("#PinCodeId").val();

    if (preloadedCountryId) {
        // Preload country name
        fetchAndSetFieldValue("/api/Company/GetCountryName", { id: preloadedCountryId }, "#CountryId", "name");

        // Enable PinCodeId since a country is preloaded
        $("#StateId").prop("disabled", false);
        $("#PinCodeId").prop("disabled", false);

        // Preload details if Pincode is provided
        if (preloadedPincodeId) {
            preloadPincodeDetails(preloadedCountryId, preloadedPincodeId);
        }
    }
    $("#CountryId").on("input change", function () {
        const selectedCountryId = $("#SelectedCountryId").val();

        if (selectedCountryId) {
            // Enable PinCodeId if a valid country is selected
            stateAutocomplete();
            $("#StateId").prop("disabled", false);
            $("#DistrictId").prop("disabled", false);
            $("#PinCodeId").prop("disabled", false);
        } else {
            // Disable PinCodeId if no country is selected
            $("#StateId").prop("disabled", true);
            $("#PinCodeId").prop("disabled", true);

            // Reset dependent fields
            resetField("#PinCodeId");
            resetField("#StateId");
            resetField("#DistrictId");
        }
    });

    $("#StateId").on("blur", function () {
        const stateIdValue = $(this).val().trim();

        if (!stateIdValue) {
            // Disable PinCodeId and clear its value
            $("#PinCodeId").prop("disabled", true);
            resetField("#DistrictId");
            resetField("#PinCodeId");
        }
        else {
            districtAutocomplete();
            $("#StateId").prop("disabled", false);
            $("#DistrictId").prop("disabled", false);
            $("#PinCodeId").prop("disabled", false);
        }
    });

    // Watch for changes in CountryId
    $("#StateId").on("input change", function () {
        const selectedStateId = $("#SelectedStateId").val();

        if (selectedStateId) {
            $("#DistrictId").prop("disabled", false);
            $("#PinCodeId").prop("disabled", false);
            // Enable PinCodeId if a valid country is selected
            districtAutocomplete();

        } else {
            // Disable PinCodeId if no country is selected
            $("#PinCodeId").prop("disabled", true);

            // Reset dependent fields
            resetField("#PinCodeId");
            resetField("#DistrictId");
        }
    });

    $("#StateId").on("blur", function () {
        const stateValue = $(this).val().trim();
        const countryId = $("#SelectedCountryId").val();
        validateStateSelection(stateValue, countryId);
    });

    $("#DistrictId").on("blur", function () {
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
                $("#PinCodeId").val(response.pincodeName);
                $("#SelectedPincodeId").val(response.pincodeId);

                // Preload State
                if (response.stateId) {
                    fetchAndSetFieldValue(
                        "/api/Company/GetStateName",
                        { id: response.stateId, CountryId: preloadedCountryId },
                        "#StateId",
                        "stateName"
                    );
                }

                // Preload District
                if (response.districtId) {
                    fetchAndSetFieldValue(
                        "/api/Company/GetDistrictName",
                        { id: response.districtId, stateId: response.stateId, CountryId: preloadedCountryId },
                        "#DistrictId",
                        "districtName"
                    );
                }
            } else {
                $("#PinCodeId").addClass("invalid")
                resetField("#PinCodeId");
                resetField("#StateId");
                resetField("#DistrictId");
            }
        },
        error: function () {
            hideLoader("#pincode-loading");
            $("#PinCodeId").addClass("invalid")
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
                $("#PinCodeId").prop("disabled", false).attr("placeholder", "Search Pincode or name ...");

                // Reset dependent fields (State, District, Pincode) when the country changes
                resetField("#StateId");
                resetField("#SelectedStateId");
                resetField("#DistrictId");
                resetField("#SelectedDistrictId");
                resetField("#PinCodeId");
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
        resetField("#StateId");
        resetField("#SelectedStateId");
        resetField("#DistrictId");
        resetField("#SelectedDistrictId");
        resetField("#PinCodeId");
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
                $("#PinCodeId").prop("disabled", false).attr("placeholder", "Search Pincode or name ...");
                $("#PinCodeId").focus();
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
    const pinCodeField = "#PinCodeId";
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

// Fetch Pincode suggestions from the server
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

// Populate fields based on the selected Pincode
function populatePincodeDetails(selectedItem) {
    $("#PinCodeId").val(selectedItem.label);
    $("#SelectedPincodeId").val(selectedItem.value);
    $("#StateId").removeClass("invalid");
    $("#StateId").val(selectedItem.stateName);
    $("#SelectedStateId").val(selectedItem.stateId);
    $("#DistrictId").removeClass("invalid");
    $("#DistrictId").val(selectedItem.districtName);
    $("#SelectedDistrictId").val(selectedItem.districtId);
    $("#PinCodeId").removeClass("invalid");
}

// Validate if the input matches any valid Pincode suggestion
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
        markInvalidField("#StateId");
        return;
    }

    $.ajax({
        url: "/api/Company/SearchState",
        type: "GET",
        data: { term: inputValue, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item => item.stateName === inputValue);

            if (!isValid) {
                markInvalidField("#StateId");
                clearStateFields();
            } else {
                $("#StateId").removeClass("invalid");
            }
        },
        error: function () {
            markInvalidField("#StateId");
            clearStateFields();
        }
    });
}

function clearStateFields() {
    $("#StateId").val("");
    $("#SelectedStateId").val("");
    $("#DistrictId").val("");
    $("#SelectedDistrictId").val("");
    $("#PinCodeId").val("");
    $("#SelectedPincodeId").val("");
}

function validateDistrictSelection(inputValue, stateId, countryId) {
    if (!inputValue) {
        clearDistrictFields();
        markInvalidField("#DistrictId");
        return;
    }

    $.ajax({
        url: "/api/Company/SearchDistrict",
        type: "GET",
        data: { term: inputValue, stateId: stateId, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item => item.districtName === inputValue);

            if (!isValid) {
                markInvalidField("#DistrictId");
                clearDistrictFields();
            } else {
                $("#DistrictId").removeClass("invalid");
            }
        },
        error: function () {
            markInvalidField("#DistrictId");
            clearDistrictFields();
        }
    });
}

function clearDistrictFields() {
    $("#DistrictId").val("");
    $("#SelectedDistrictId").val("");
    $("#PinCodeId").val("");
    $("#SelectedPincodeId").val("");
}

// Mark a field as invalid
function markInvalidField(fieldSelector) {
    $(fieldSelector).addClass("invalid");
}

// Clear Pincode and dependent fields
function clearPincodeFields() {
    $("#PinCodeId").val("");
    $("#SelectedPincodeId").val("");
    $("#StateId").val("");
    $("#SelectedStateId").val("");
    $("#DistrictId").val("");
    $("#SelectedDistrictId").val("");
}


function stateAutocomplete() {
    $("#StateId").autocomplete({
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
            $("#StateId").val(ui.item.label);
            $("#SelectedStateId").val(ui.item.value);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            $("#StateId").val(ui.item.label);
            $("#SelectedStateId").val(ui.item.value);

            return false;
        },
        minLength: 2
    });
}

function districtAutocomplete() {
    $("#DistrictId").autocomplete({
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
            $("#DistrictId").val(ui.item.label);
            $("#SelectedDistrictId").val(ui.item.value);

            return false;
        },
        select: function (event, ui) {
            $("#DistrictId").val(ui.item.label);
            $("#SelectedDistrictId").val(ui.item.value);

            return false;
        },
        minLength: 2
    });
}

// Utility functions
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

function clearPinCodeField() {
    var pincodeDropdown = document.getElementById("PinCodeId");
    if (pincodeDropdown) {
        // Clear all selected options
        pincodeDropdown.value = '';
        // Optionally, you can clear the options as well
        pincodeDropdown.innerHTML = '';
    }
}