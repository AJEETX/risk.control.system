$(document).ready(function () {
    const fields = ['#CountryId', '#StateId', '#DistrictId', '#PinCodeId'];

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

        // Preload details if Pincode is provided
        if (preloadedPincodeId) {
            preloadPincodeDetails(preloadedCountryId, preloadedPincodeId);
        }
    }

    // Initialize autocomplete for Pincode
    pincodeAutocomplete();

    // Dynamically fetch State and District on Pincode change
    $("#PinCodeId").on("change", function () {
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
                            label: `${item.pincode} - ${item.name}`,
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
