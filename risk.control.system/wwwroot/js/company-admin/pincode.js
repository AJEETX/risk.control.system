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

    // On CountryId blur, check if it's cleared and disable PinCodeId
    $("#CountryId").on("blur", function () {
        const countryIdValue = $(this).val().trim();

        if (!countryIdValue) {
            // Disable PinCodeId and clear its value
            $("#StateId").prop("disabled", true);
            $("#PinCodeId").prop("disabled", true);
            resetField("#PinCodeId");
            resetField("#StateId");
        }
    });

    // Watch for changes in CountryId
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

    // Initialize country autocomplete
    countryAutocomplete();

    //Initialize state autocomplete
    stateAutocomplete();

    //Initialize district autocomplete
    districtAutocomplete();

    // Initialize autocomplete for Pincode
    pincodeAutocomplete();

    // Dynamically fetch State and District on Pincode change
    $("#PinCodeId").on("blur input, change", function () {
        const selectedpinCodeId = $("#SelectedStateId").val();
        pincodeAutocomplete();
    });

    $("#PinCodeId").on("autocompletechange", function (event, ui) {
        if (!ui.item) {
            // If no valid item is selected, clear the input
            $(this).val("");
            $("#SelectedPincodeId").val("");
        }
    });
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
                resetField("#PinCodeId");
                resetField("#StateId");
                resetField("#DistrictId");
            }
        },
        error: function () {
            hideLoader("#pincode-loading");
            alert("Error fetching Pincode details.");
        }
    });
}

function countryAutocomplete() {
    $("#CountryId").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/api/Company/GetCountrySuggestions", // API endpoint for country suggestions
                type: "GET",
                data: { term: request.term },
                success: function (data) {
                    response(data); // Pass the list of suggestions to the autocomplete
                },
                error: function () {
                    alert("Error fetching country suggestions.");
                }
            });
        },
        select: function (event, ui) {
            // On selecting a country, populate the field with its name and set its ID
            $("#CountryId").val(ui.item.label); // Set country name
            $("#SelectedCountryId").val(ui.item.id); // Set hidden field for CountryId
            $("#PinCodeId").prop("disabled", false).attr("placeholder", "Type Pincode or name");

            // Reset dependent fields (State, District, Pincode) when the country changes
            resetField("#StateId");
            resetField("#SelectedStateId");
            resetField("#DistrictId");
            resetField("#SelectedDistrictId");
            resetField("#PinCodeId");
            resetField("#SelectedPincodeId");

            return false; // Prevent default autocomplete behavior
        },
        minLength: 2 // Minimum number of characters to trigger suggestions
    });
}

function pincodeAutocomplete() {
    const pinCodeField = "#PinCodeId";
    const selectedPinCodeField = "#SelectedPincodeId";
    const selectedCountryField = "#SelectedCountryId";

    // Initialize autocomplete
    $(pinCodeField).autocomplete({
        source: function (request, response) {
            fetchPincodeSuggestions(request.term, $(selectedCountryField).val(), response);
        },
        focus: function (event, ui) {
            // Set the input field to the "label" value when navigating with arrow keys
            $(pinCodeField).val(ui.item.label);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
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
            responseCallback(suggestions);
        },
        error: function () {
            alert("Error fetching Pincode suggestions.");
        }
    });
}

// Populate fields based on the selected Pincode
function populatePincodeDetails(selectedItem) {
    $("#PinCodeId").val(selectedItem.label);
    $("#SelectedPincodeId").val(selectedItem.value);
    $("#StateId").val(selectedItem.stateName);
    $("#SelectedStateId").val(selectedItem.stateId);
    $("#DistrictId").val(selectedItem.districtName);
    $("#SelectedDistrictId").val(selectedItem.districtId);
    $("#PinCodeId").removeClass("invalid");
}

// Validate if the input matches any valid Pincode suggestion
function validatePincodeSelection(inputValue, countryId) {
    if (!inputValue) {
        clearPincodeFields();
                markInvalidField("#PinCodeId");
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
                markInvalidField("#PinCodeId");
                clearPincodeFields();
                //alert("Please select a valid Pincode from the dropdown.");
            } else {
                $("#PinCodeId").removeClass("invalid"); // Remove invalid class if valid
            }
        },
        error: function () {
            alert("Error validating Pincode.");
        }
    });
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
                    alert("Error fetching Pincode suggestions.");
                }
            });
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
                data: { term: request.term, stateId: $("#SelectedStateId").val() ,countryId: $("#SelectedCountryId").val() },
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
                    alert("Error fetching Pincode suggestions.");
                }
            });
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
            alert("Error loading data.");
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
