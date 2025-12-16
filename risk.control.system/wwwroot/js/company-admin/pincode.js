$(document).ready(function () {
    const fields = ['#CountryText', '#StateText', '#DistrictText', '#PinCodeText'];
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

    const preloadedCountryId = $("#SelectedCountryId").val();
    const preloadedPincodeId = $("#SelectedPincodeId").val();

    if (preloadedCountryId) {
        // Preload country name
        fetchAndSetFieldValue("/api/MasterData/GetCountryName", { id: preloadedCountryId }, "#CountryText", "name");

        $("#StateText").prop("disabled", false);
        $("#PinCodeText").prop("disabled", false);

        // Preload details if Pincode is provided
        if (preloadedPincodeId) {
            preloadPincodeDetails(preloadedCountryId, preloadedPincodeId);
        }
    }
    $("#CountryText").on("input change", function () {
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
        url: "/api/MasterData/GetPincode",
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
                        "/api/MasterData/GetStateName",
                        { id: response.stateId, CountryId: preloadedCountryId },
                        "#StateText",
                        "stateName"
                    );
                }

                // Preload District
                if (response.districtId) {
                    fetchAndSetFieldValue(
                        "/api/MasterData/GetDistrictName",
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
    $("#CountryText").autocomplete({
        source: function (request, response) {
            fetchCountrySuggestions(request.term, function (suggestions) {
                // Filter out non-selectable items
                response(suggestions);
            });
        },
        focus: function (event, ui) {
            if (ui.item.isSelectable === false) {
                $("#CountryText").val('');
                $("#CountryText").addClass("invalid");
                return false;
            }
            // Set the input field to the "label" value when navigating with arrow keys
            $("#CountryText").val(ui.item.label);
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            if (ui.item.isSelectable === false) {
                // Prevent selection if it's the "No result found"
                $("#CountryText").addClass("invalid");
                return false;
            } else {
                $("#CountryText").removeClass("invalid");
                // On selecting a country, populate the field with its name and set its ID
                $("#CountryText").val(ui.item.label); // Set country name
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
    $("#CountryText").on("blur", function () {
        validateCountrySelection($(this).val(), $("#SelectedCountryId").val());
    });
}
function fetchCountrySuggestions(term, responseCallback) {
    $.ajax({
        url: "/api/MasterData/GetCountrySuggestions", // API endpoint for country suggestions
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
        $("#CountryText").val("");
        $("#SelectedCountryId").val("");
        markInvalidField("#CountryText");
        resetField("#StateText");
        resetField("#SelectedStateId");
        resetField("#DistrictText");
        resetField("#SelectedDistrictId");
        resetField("#PinCodeText");
        resetField("#SelectedPincodeId");
        return;
    }

    $.ajax({
        url: "/api/MasterData/GetCountrySuggestions",
        type: "GET",
        data: { term: inputValue},
        success: function (data) {
            const isValid = data.some(item =>
                `${item.name}` === inputValue);

            if (!isValid) {
                markInvalidField("#CountryText");
                $("#CountryText").val("");
                $("#SelectedCountryId").val("");
                $("#CountryText").focus();
            } else {
                $("#PinCodeText").prop("disabled", false).attr("placeholder", "Search Pincode or name ...");
                $("#PinCodeText").focus();
                $("#CountryText").removeClass("invalid"); // Remove invalid class if valid
            }
        },
        error: function () {
            console.log("Error validating Country.");
            markInvalidField("#CountryText");
            $("#CountryText").val("");
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
        url: "/api/MasterData/GetPincodeSuggestions",
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
        markInvalidField("#CountryText");
        return;
    }

    $.ajax({
        url: "/api/MasterData/GetPincodeSuggestions",
        type: "GET",
        data: { term: inputValue, countryId: countryId },
        success: function (data) {
            const isValid = data.some(item =>
                `${item.name} - ${item.pincode}` === inputValue);

            if (!isValid) {
                markInvalidField("#CountryText");
                clearPincodeFields();
                //alert("Please select a valid Pincode from the dropdown.");
            } else {
                var countryIdVisible = $("#CountryText");
                if (countryIdVisible) {
                    $("#CountryText").removeClass("invalid"); // Remove invalid class if valid
                }
            }
        },
        error: function () {
            console.log("Error validating Pincode.");
            markInvalidField("#CountryText");
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
        url: "/api/MasterData/SearchState",
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
        url: "/api/MasterData/SearchDistrict",
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
                url: "/api/MasterData/SearchState",
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
                url: "/api/MasterData/SearchDistrict",
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
