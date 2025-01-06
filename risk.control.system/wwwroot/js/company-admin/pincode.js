$(document).ready(function () {
    const fields = ['#CountryId', '#StateId', '#DistrictId', '#PinCodeId'];
    // Disable PinCodeId by default
    $("#PinCodeId").prop("disabled", true).attr("placeholder", "");
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
    const preloadedPincodeId = $("#SelectedPincodeId").val();

    if (preloadedCountryId) {
        // Preload country name
        fetchAndSetFieldValue("/api/Company/GetCountryName", { id: preloadedCountryId }, "#CountryId", "name");

        // Enable PinCodeId since a country is preloaded
            $("#PinCodeId").prop("disabled", false).attr("placeholder", "Type Pincode or name");

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
            $("#PinCodeId").prop("disabled", true).val("").attr("placeholder", "");
            resetField("#PinCodeId");
            resetField("#StateId");
            resetField("#DistrictId");
        }
    });
    // Watch for changes in CountryId
    $("#CountryId").on("input change", function () {
        const selectedCountryId = $("#SelectedCountryId").val();

        if (selectedCountryId) {
            // Enable PinCodeId if a valid country is selected
            $("#PinCodeId").prop("disabled", false).attr("placeholder", "Type Pincode or name");
        } else {
            // Disable PinCodeId if no country is selected
            $("#PinCodeId").prop("disabled", true).val("").attr("placeholder", "");

            // Reset dependent fields
            resetField("#PinCodeId");
            resetField("#StateId");
            resetField("#DistrictId");
        }
    });

    // Initialize country autocomplete
    countryAutocomplete();
    // Initialize autocomplete for Pincode
    pincodeAutocomplete();

    // Dynamically fetch State and District on Pincode change
    $("#PinCodeId").on("input, change", function () {
        pincodeAutocomplete();
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
    $("#PinCodeId").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/api/Company/GetPincodeSuggestions",
                type: "GET",
                data: { term: request.term, countryId: $("#SelectedCountryId").val() },
                success: function (data) {
                    response(data.map(function (item) {
                        return {
                            label: `${item.name} - ${item.pincode}`,
                            value: item.pincodeId,
                            stateId: item.stateId,
                            stateName: item.stateName,
                            districtId: item.districtId,
                            districtName: item.districtName
                        };
                    }));
                },
                error: function () {
                    alert("Error fetching Pincode suggestions.");
                }
            });
        },
        select: function (event, ui) {
            $("#PinCodeId").val(ui.item.label);
            $("#SelectedPincodeId").val(ui.item.value);
            $("#StateId").val(ui.item.stateName);
            $("#SelectedStateId").val(ui.item.stateId);
            $("#DistrictId").val(ui.item.districtName);
            $("#SelectedDistrictId").val(ui.item.districtId);

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
