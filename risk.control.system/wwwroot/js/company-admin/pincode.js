$(document).ready(function () {
    // Initialize placeholders and field validations
    updatePlaceholdersBasedOnState();
    initializeFieldValidations();

    // Preload data if available
    preloadFieldData();

    // Initialize autocomplete for fields
    initializeAutocomplete();

    // Event listener for input focus to select text
    $('input.auto-dropdown').on('focus', function () {
        $(this).select();
    });
});

/**
 * Preloads field data from hidden fields (e.g., SelectedCountryId).
 */
function preloadFieldData() {
    const preloadedCountryId = $("#SelectedCountryId").val();
    if (preloadedCountryId) {
        console.log("Preloaded Country ID: ", preloadedCountryId);
        // Fetch and set the Country field value
        fetchAndSetFieldValue("/api/Company/GetCountryName", { id: preloadedCountryId }, "#CountryId", "name", () => {
            // After Country is loaded, load State, District, and Pincode based on preloaded values
            loadStateData(preloadedCountryId);
        });
    }
}

/**
 * Loads state data based on the preloaded Country ID.
 */
function loadStateData(countryId) {
    const preloadedStateId = $("#SelectedStateId").val();
    if (preloadedStateId) {
        console.log("Preloaded State ID: ", preloadedStateId);
        fetchAndSetFieldValue("/api/Company/GetStateName", { id: preloadedStateId, countryId: countryId }, "#StateId", "stateName", () => {
            // After State is loaded, load District and Pincode based on preloaded values
            loadDistrictData(countryId, preloadedStateId);
        });
    }
}

/**
 * Loads district data based on the preloaded Country and State IDs.
 */
function loadDistrictData(countryId, stateId) {
    const preloadedDistrictId = $("#SelectedDistrictId").val();
    if (preloadedDistrictId) {
        console.log("Preloaded District ID: ", preloadedDistrictId);
        fetchAndSetFieldValue("/api/Company/GetDistrictName", { id: preloadedDistrictId, stateId: stateId, countryId: countryId }, "#DistrictId", "districtName", () => {
            // After District is loaded, load Pincode based on preloaded values
            loadPincodeData(countryId, stateId, preloadedDistrictId);
        });
    }
}

/**
 * Loads pincode data based on the preloaded Country, State, and District IDs.
 */
function loadPincodeData(countryId, stateId, districtId) {
    const preloadedPinCodeId = $("#SelectedPincodeId").val();
    if (preloadedPinCodeId) {
        console.log("Preloaded PinCode ID: ", preloadedPinCodeId);
        fetchAndSetFieldValue("/api/Company/GetPincodeName", { id: preloadedPinCodeId, districtId: districtId, stateId: stateId, countryId: countryId }, "#PinCodeId", "pincodeName");
    }
}

/**
 * Fetches a value from the server and sets it to the specified input field.
 */
function fetchAndSetFieldValue(url, data, inputSelector, responseKey, callback) {
    const $inputWrapper = $(inputSelector).closest('.input-group');  // Get the input container
    const $spinner = $inputWrapper.find('.loading-spinner');         // Find the spinner inside the input container
    const $inputField = $(inputSelector); // Target the input field itself

    if ($spinner.length) {
        $spinner.addClass('active'); // Show spinner
    }

    $.ajax({
        url,
        type: "GET",
        data,
        success: function (response) {
            if (response && response[responseKey]) {
                $inputField.val(response[responseKey]);
                // Fade in the input field
                $inputField.hide().fadeIn(1000); // Adjust duration as needed
                if (callback) callback();
            }
        },
        error: function () {
            console.error(`Failed to fetch value for ${inputSelector}`);
        },
        complete: function () {
            if ($spinner.length) {
                $spinner.removeClass('active'); // Hide spinner after the request is complete
            }
        }
    });
}

/**
 * Initializes autocomplete for all relevant fields.
 */
function initializeAutocomplete() {
    const autocompleteConfig = [
        {
            field: "#CountryId",
            url: "/api/Company/SearchCountry",
            onSelect: (ui) => handleAutocompleteSelect(ui, "#CountryId", "#SelectedCountryId", ["#StateId", "#DistrictId", "#PinCodeId"])
        },
        {
            field: "#StateId",
            url: "/api/Company/SearchState",
            extraData: () => ({ countryId: $("#SelectedCountryId").val() }),
            onSelect: (ui) => handleAutocompleteSelect(ui, "#StateId", "#SelectedStateId", ["#DistrictId", "#PinCodeId"])
        },
        {
            field: "#DistrictId",
            url: "/api/Company/SearchDistrict",
            extraData: () => ({
                countryId: $("#SelectedCountryId").val(),
                stateId: $("#SelectedStateId").val()
            }),
            onSelect: (ui) => handleAutocompleteSelect(ui, "#DistrictId", "#SelectedDistrictId", ["#PinCodeId"])
        },
        {
            field: "#PinCodeId",
            url: "/api/Company/SearchPincode",
            extraData: () => ({
                countryId: $("#SelectedCountryId").val(),
                stateId: $("#SelectedStateId").val(),
                districtId: $("#SelectedDistrictId").val()
            }),
            onSelect: (ui) => handleAutocompleteSelect(ui, "#PinCodeId", "#SelectedPincodeId")
        }
    ];

    autocompleteConfig.forEach(config => {
        setAutocomplete(config.field, config.url, config.extraData || (() => ({})), config.onSelect);
    });
}

/**
 * Handles selection in autocomplete and updates dependent fields.
 */
function handleAutocompleteSelect(ui, inputSelector, hiddenSelector, dependentFields = []) {
    $(inputSelector).val(ui.item.label);
    $(hiddenSelector).val(ui.item.id);
    dependentFields.forEach(field => resetField(field));
}

/**
 * Sets up an autocomplete field with dynamic data fetching.
 */
function setAutocomplete(fieldSelector, url, extraDataCallback, onSelectCallback) {
    const $wrapper = $(fieldSelector).closest('.autocomplete-wrapper');
    const $spinner = $wrapper.find('.loading-spinner');

    $(fieldSelector).autocomplete({
        source: function (request, response) {
            if ($spinner.length) {
                $spinner.show(); // Show spinner while fetching data
            }

            $.ajax({
                url,
                data: { term: request.term, ...extraDataCallback() },
                success: function (data) {
                    response(data.map(item => ({
                        label: item.name || item.stateName || item.districtName || item.pincodeName,
                        value: item.name || item.stateName || item.districtName || item.pincodeName,
                        id: item.id || item.stateId || item.districtId || item.pincodeId
                    })));
                },
                error: function () {
                    response([{ label: "Error fetching data", value: "" }]);
                },
                complete: function () {
                    if ($spinner.length) {
                        $spinner.hide(); // Hide spinner after fetching
                    }
                }
            });
        },
        minLength: 0,
        select: function (event, ui) {
            if (ui.item.value) {
                onSelectCallback(ui);
            }
            return false;
        }
    });
    // Trigger autocomplete on focus if the input field is empty
    $(fieldSelector).on("focus", function () {
        const value = $(this).val().trim();
        if (!value) {
            $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
        }
    });
}

/**
 * Resets a field's value and clears its hidden field if applicable.
 */
function resetField(fieldSelector, hiddenFieldSelector = null) {
    $(fieldSelector).val('');
    if (hiddenFieldSelector) $(hiddenFieldSelector).val('');
}

/**
 * Updates placeholders dynamically based on field state.
 */
function updatePlaceholdersBasedOnState() {
    $(".form-control.auto-dropdown").each(function () {
        const $field = $(this);
        const placeholder = $field.data("placeholder"); // Get the placeholder value
        if (placeholder) {
            $field.attr("placeholder", `Type ${placeholder}`);
            console.log(`Updated placeholder for #${$field.attr("id")} to "Type ${placeholder}"`);
        } else {
            console.warn(`No placeholder found for #${$field.attr("id")}`);
        }
    });
}

/**
 * Initializes field validation and toggling for dependencies.
 */
function initializeFieldValidations() {
    handleFieldValidation("#CountryId", ["#StateId", "#DistrictId", "#PinCodeId"]);
    handleFieldValidation("#StateId", ["#DistrictId", "#PinCodeId"]);
    handleFieldValidation("#DistrictId", ["#PinCodeId"]);
}

/**
 * Validates a parent field and toggles its dependent fields based on validity.
 */
function handleFieldValidation(parentFieldId, dependentFieldIds) {
    let initialValue = ""; // Store the initial value of the field on focus

    $(parentFieldId)
        .on("focus", function () {
            initialValue = $(this).val().trim(); // Store the value when the field gains focus
        })
        .on("blur", function () {
            const currentValue = $(this).val().trim();

            // If the value hasn't changed, skip resetting dependent fields
            if (currentValue === initialValue) {
                console.log(`No change detected in ${parentFieldId}`);
                return;
            }

            // Update dependent fields only if the value changes
            const isValid = validateAutocompleteValue(parentFieldId);
            toggleDependentFields(dependentFieldIds, isValid);
        });
}

function validateAutocompleteValue(fieldSelector) {
    const $field = $(fieldSelector);
    const value = $field.val().trim();
    const autocomplete = $field.data("ui-autocomplete");
    if (!autocomplete) return false;

    const options = autocomplete.options.source;
    if (typeof options === "function") {
        let isValid = false;
        options({ term: value }, function (data) {
            isValid = data.some(option => option.label === value);
        });
        return isValid;
    } else if (Array.isArray(options)) {
        return options.some(option => option.label === value);
    }
    return false;
}


/**
 * Toggles dependent fields based on parent field validity.
 */
function toggleDependentFields(dependentFieldIds, isValid) {
    dependentFieldIds.forEach(fieldId => {
        if (!isValid) {
            $(fieldId).val('');
        }
    });
}
