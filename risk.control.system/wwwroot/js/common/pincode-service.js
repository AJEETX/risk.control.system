$("#CountryId").val('');
function GetRemainingServicePinCode(showDefaultOption = true) {
    var pinCode = $('#PinCodeId');
    var pinCodeId = $('#SelectedMultiPincodeId');

    var state = document.getElementById('StateId').value;
    var stateId = document.getElementById('SelectedStateId').value;

    var district = document.getElementById('DistrictId').value;
    var districtId = document.getElementById('SelectedDistrictId').value;

    if (state == '' || stateId == '' || district == '' || districtId == '') {
        $(pinCode).empty();
        $(pinCode).val('');
    }
    else {
        var lobId = document.getElementById('LineOfBusinessId').value;
        var serviceId = document.getElementById('InvestigationServiceTypeId').value;
        var vendorId = document.getElementById('vendorId').value;

        $.get("/api/MasterData/GetPincodesByDistrictIdWithoutPreviousSelectedService",
            {
                stateId: stateId,
                districtId: districtId,
                district: district,
                vendorId: vendorId,
                lobId: lobId,
                serviceId: serviceId
            },
            function (data) {
            PopulatePinCode("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
        });
    }
}

function PopulatePinCode(dropDownId, list, option, showDefaultOption) {
    $(dropDownId).empty();
    $(dropDownId).val('');
    if (showDefaultOption)
        $(dropDownId).append(option)
    if (list == '' || list && list.length == 0) {
        $('#DistrictId').empty();
        $('#DistrictId').val('');
        $('#DistrictId').focus();
        $.alert({
            title: "SERVICE EXISTS",
            content: "Please select other district.",
            icon: 'fas fa-exclamation-triangle',
            type: 'red',
            closeIcon: true,
            buttons: {
                ok: {
                    text: "OK",
                    btnClass: 'btn-danger',
                    action: function () {
                        $('#DistrictId').focus(); // Focus on the input element after clicking OK
                    }
                }
            }
        });
    }
    else if (list && list.length == 1 && list[0].pinCodeId == -1) {
        $(dropDownId).empty(); // Clear existing options
        $(dropDownId).append("<option class='all-pincodes' value='-1'> -- ALL PIN CODES -- </option>"); // Add "ALL PIN CODES" option

        // Setting the value to '-1' to select the "ALL PIN CODES" option
        $(dropDownId).val('-1'); // This will set the "ALL PIN CODES" option as selected
        $(dropDownId).trigger('change'); // Trigger change event if you want to do any additional processing after selection


        $.alert({
            title: "ALL PIN CODES",
            content: "District ALL PIN CODES selected.",
            icon: 'fa fa-info-circle', // Using a more specific icon
            type: 'info', // Info type alert
            closeIcon: true, // Show close icon
            buttons: {
                ok: {
                    text: "OK",
                    btnClass: 'btn-info', // Large button style
                    action: function () {
                        $(dropDownId).focus();
                    }
                }
            },
            backgroundDismiss: true, // Allow dismiss by clicking outside the box
            contentTextAlign: 'center', // Center the text content
            autoClose: '3000', // Automatically close after 3 seconds
            onOpen: function () {
                // Optional: Add custom animations or additional functionality when the alert opens
                $(this).addClass('animated fadeIn'); // Adding a fade-in animation
            },
            onClose: function () {
                // Optional: Add custom functionality when the alert closes
                $(this).removeClass('animated fadeIn'); // Remove fade-in animation when closed
            }
        });
    }
    else if (list && list.length > 0) {
        $.each(list, function (index, row) {
            $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + " -- " + row.code + "</option>");
            //$('#create').prop('disabled', false);
        });
    }
     
}
function PopulateInvestigationServices(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.name + "</option>")
    });
}

$(document).ready(function () {

    $("#DistrictId").on("blur change", function () {
        // Call the GetRemainingServicePinCode function with the necessary parameters
        GetRemainingServicePinCode(false);
    });

    // Bind the change event to the dropdown
    $("#LineOfBusinessId").on("change", function () {
        var value = $(this).val();

        if (value === '') {
            // Clear and reset InvestigationServiceTypeId dropdown
            $('#InvestigationServiceTypeId').empty();
            $('#InvestigationServiceTypeId').append("<option value=''>--- SELECT ---</option>");
        } else {
            // Fetch investigation services via AJAX and populate the dropdown
            $.get("/api/MasterData/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
                PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--- SELECT ---</option>");
            });
        }
    });

    const inputSelector = "#SelectedCountryId";
    const preloadedCountryId = $("#SelectedCountryId").val();
    const $inputWrapper = $(inputSelector).closest('.input-group');  // Get the input container
    const $spinner = $inputWrapper.find('.loading-spinner');
    if ($spinner.length) {
        $spinner.addClass('active'); // Show spinner
    }

    const fields = ['#CountryId', '#StateId', '#DistrictId'];

    fields.forEach(function (field) {
        $(field).on('input', function () {
            const isValid = /^[a-zA-Z0-9 ]*$/.test(this.value); // Check if input is valid
            if (!isValid) {
                $(this).val('');
                //$(this).addClass('invalid'); // Add error class
            } else {
                //$(this).removeClass('invalid'); // Remove error class
            }
        });
    });

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
    var preloadedDistrictId = $("#SelectedDistrictId").val();
    const districtId = $("#DistrictId").val();
    if (preloadedDistrictId || !districtId) {
        if (!preloadedDistrictId && !districtId) {
            preloadedDistrictId = -1;
        }
        console.log("Preloaded District ID: ", preloadedDistrictId);
        fetchAndSetFieldValue("/api/Company/GetDistrictNameForAgency",
            {
                id: preloadedDistrictId,
                stateId: stateId,
                countryId: countryId,
                lob: $("#LineOfBusinessId").val(),
                serviceId: $("#InvestigationServiceTypeId").val(),
                vendorId: $("#vendorId").val()
            }, "#DistrictId", "districtName", () => {
            // After District is loaded, load Pincode based on preloaded values
            //loadPincodeData(countryId, stateId, preloadedDistrictId);
        });
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
                $inputField.removeClass('invalid');
                // Fade in the input field
                //$inputField.hide().fadeIn(1000); // Adjust duration as needed
                if (callback) callback();
            }
        },
        error: function () {
            $inputField.addClass('invalid');
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
    const countryDependentFields = ["#StateId", "#DistrictId", "#PinCodeId"];
    const stateDependentFields = ["#DistrictId", "#PinCodeId"];
    const districtDependentFields = ["#PinCodeId"];
    const autocompleteConfig = [
        {
            field: "#CountryId",
            url: "/api/Company/SearchCountry",
            onSelect: (ui) => handleAutocompleteSelect(ui, "#CountryId", "#SelectedCountryId", countryDependentFields),
            dependentFields: countryDependentFields
        },
        {
            field: "#StateId",
            url: "/api/Company/SearchState",
            extraData: () => ({ countryId: $("#SelectedCountryId").val() }),
            onSelect: (ui) => handleAutocompleteSelect(ui, "#StateId", "#SelectedStateId", stateDependentFields),
            dependentFields: stateDependentFields
        },
        {
            field: "#DistrictId",
            url: "/api/Company/SearchDistrict",
            extraData: () => ({
                countryId: $("#SelectedCountryId").val(),
                stateId: $("#SelectedStateId").val()
            }),
            onSelect: (ui) => handleAutocompleteSelect(ui, "#DistrictId", "#SelectedDistrictId", districtDependentFields),
            dependentFields: districtDependentFields
        },
        //{
        //    field: "#PinCodeId",
        //    url: "/api/Company/SearchPincode",
        //    extraData: () => ({
        //        countryId: $("#SelectedCountryId").val(),
        //        stateId: $("#SelectedStateId").val(),
        //        districtId: $("#SelectedDistrictId").val()
        //    }),
        //    onSelect: (ui) => handleAutocompleteSelect(ui, "#PinCodeId", "#SelectedPincodeId")
        //}
    ];

    autocompleteConfig.forEach(config => {
        setAutocomplete(config.field, config.url, config.extraData || (() => ({})), config.onSelect, config.dependentFields);
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
function setAutocomplete(fieldSelector, url, extraDataCallback, onSelectCallback, dependentFields) {
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
                    if (!data || data.length === 0) {
                        console.warn("No results found for term:", request.term);
                        response([{ label: "No results found", value: "" }]);
                    } else {
                        response(data.map(item => ({
                            label: item.name || item.stateName || item.districtName || item.pincodeName,
                            value: item.name || item.stateName || item.districtName || item.pincodeName,
                            id: item.id || item.stateId || item.districtId || item.pincodeId
                        })));
                    }
                },
                error: function (xhr) {
                    console.error("Error fetching autocomplete data:", error);
                    console.log("Response Text:", xhr.responseText); // Log the server error
                    $(fieldSelector).addClass('invalid');
                    const hiddenFieldSelector = $(fieldSelector).data('hiddenField');
                    if (hiddenFieldSelector) {
                        $(hiddenFieldSelector).val('');
                    }
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
        focus: function (event, ui) {
            if (ui.item.label == 'Error fetching data' || ui.item.label == "No results found") {
                $(fieldSelector).val('');
                $(fieldSelector).addClass('invalid');
                const hiddenFieldSelector = $(fieldSelector).data('hiddenField');
                if (hiddenFieldSelector) {
                    $(hiddenFieldSelector).val('');
                }
            }
            else {
                // Set the input field to the "label" value when navigating with arrow keys
                $(fieldSelector).val(ui.item.label);
                $(fieldSelector).removeClass('invalid');
                const hiddenFieldSelector = $(fieldSelector).data('hiddenField');
                if (hiddenFieldSelector) {
                    $(hiddenFieldSelector).val(ui.item.id);
                }
            }
            return false; // Prevent default behavior of updating the field with "value"
        },
        select: function (event, ui) {
            if (ui.item.value == 'Error fetching data' || ui.item.label == "No results found") {
                $(fieldSelector).val('');
                $(fieldSelector).addClass('invalid');
                const hiddenFieldSelector = $(fieldSelector).data('hiddenField');
                if (hiddenFieldSelector) {
                    $(hiddenFieldSelector).val('');
                }
            } else {
                if (ui.item.value) {
                    // Update the input field with the selected label
                    $(fieldSelector).val(ui.item.label);

                    // Store the selected ID in a hidden input field or data attribute
                    const hiddenFieldSelector = $(fieldSelector).data('hiddenField');
                    if (hiddenFieldSelector) {
                        $(hiddenFieldSelector).val(ui.item.id);
                    }

                    // Call the provided callback function
                    if (onSelectCallback) {
                        onSelectCallback(ui);
                    }

                    // Remove the error class in case the field was previously marked invalid
                    $(fieldSelector).removeClass('invalid');
                }
            }
            return false;
        }
    });
    // Trigger autocomplete on focus if the input field is empty
    $(fieldSelector).on("focus", function () {
        const rawValue = $(this).val();
        if (rawValue == '') {
            const value = rawValue.trim();
            if (!value) {
                $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
            }
        }
    });

    // Validate the field value on blur
    $(fieldSelector).on("blur", function () {
        const $field = $(this);
        const enteredValue = $field && $field.val() !== null ? $field.val().trim() : '';
        const autocomplete = $field.data('ui-autocomplete');

        if (!enteredValue) {
            // If the current value is empty, clear all dependent fields
            if (Array.isArray(dependentFields) && dependentFields.length > 0) {
                dependentFields.forEach(selector => {
                    const $dependentField = $(selector);
                    if ($dependentField.length) {
                        $dependentField.val(''); // Clear the value of each dependent field

                        // Temporarily clear autocomplete source
                        if ($dependentField.data('ui-autocomplete')) {
                            $dependentField.autocomplete("option", "source", function (request, response) {
                                response([]); // Return an empty result
                            });
                        }
                    } else {
                        console.warn(`Dependent field selector "${selector}" did not match any elements.`);
                    }
                });
            }

            $field.addClass('invalid'); // Add invalid class if no value
            return;
        }

        if (!autocomplete) {
            return;
        }

        // Validate if the entered value matches any autocomplete option
        const options = autocomplete.options.source;

        if (typeof options === 'function') {
            // Handle async source function
            options({ term: enteredValue }, function (data) {
                const validOptions = data.filter(option =>
                    option.value !== "" &&
                    option.label !== "No results found" &&
                    option.label !== undefined
                );

                const isValid = validOptions.some(option =>
                    (option.label || "").trim().toLowerCase() === enteredValue.trim().toLowerCase()
                );

                if (!isValid) {
                    $field.val(''); // Clear invalid input
                    $field.addClass('invalid'); // Add invalid class
                } else {
                    $field.removeClass('invalid'); // Remove invalid class if valid
                }
                toggleDependentFields(dependentFields, isValid);
            });
        } else if (Array.isArray(options)) {
            // Handle static options
            const validOptions = options.filter(option =>
                option.value !== "" &&
                option.label !== "No results found" &&
                option.label !== undefined
            );

            const isValid = validOptions.some(option =>
                (option.label || "").trim().toLowerCase() === enteredValue.trim().toLowerCase()
            );

            if (!isValid) {
                $field.val(''); // Clear invalid input
                $field.addClass('invalid'); // Add invalid class
            } else {
                $field.removeClass('invalid'); // Remove invalid class if valid
            }
            toggleDependentFields(dependentFields, isValid);
        }
    });

    // Reinitialize dependent fields' autocomplete on focus
    $(dependentFields.join(',')).on("focus", function () {
        const $field = $(this);

        // Restore autocomplete source dynamically
        const originalSource = $field.data('originalSource'); // Store the original source during initialization
        if (originalSource) {
            $field.autocomplete("option", "source", originalSource);
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
    handleFieldValidation("#CountryId", ["#StateId", "#DistrictId"]);
    handleFieldValidation("#StateId", ["#DistrictId"]);
    handleFieldValidation("#DistrictId");
}

/**
 * Validates a parent field and toggles its dependent fields based on validity.
 */
function handleFieldValidation(parentFieldId, dependentFieldIds) {
    let initialValue = ""; // Store the initial value of the field on focus
    const isValid = validateAutocompleteValue(parentFieldId);
    toggleDependentFields(dependentFieldIds, isValid);
    //    });
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
    if (dependentFieldIds) {
        dependentFieldIds.forEach(fieldId => {
            if (!isValid) {
                $(fieldId).val('');
                //$(fieldId).addClass('invalid');
            }
            else {
                $(fieldId).removeClass('invalid');
            }
        });
    }
}
