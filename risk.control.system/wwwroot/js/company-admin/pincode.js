$(document).ready(function () {

    // DISTRICT SEED
    var preloadedDistrictId = $("#SelectedDistrictId").val(); // Get the hidden field value
    if (preloadedDistrictId) {
        $.ajax({
            url: '/api/Company/GetDistrictName', // Endpoint to fetch DistrictName
            type: 'GET',
            data: { districtId: preloadedDistrictId },
            success: function (response) {
                if (response && response.districtName) {
                    $("#DistrictId").val(response.districtName); // Populate input with name
                }
            },
            error: function () {
                console.error('Failed to fetch DistrictName');
            }
        });
    }

    // Autocomplete logic remains the same

    $("#DistrictId").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/api/Company/SearchDistrict',
                data: {
                    term: request.term,
                    stateId: $("#StateId").val(),
                    countryId: $("#CountryId").val()
                },
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.districtName,
                            value: item.districtName,
                            id: item.districtId
                        };
                    }));
                }
            });
        },
        minLength: 0,
        select: function (event, ui) {
            $("#DistrictId").val(ui.item.label); // Set name in input
            $("#SelectedDistrictId").val(ui.item.id);  // Set ID in hidden field
            return false;
        }
    });

    $("#DistrictId").on('input', function () {
        if (!$(this).val()) {
            $("#SelectedDistrictId").val('');
        }
    });

    //PINCODE SEED
    var preloadedPinCodeId = $("#SelectedId").val(); // Get the hidden field value

    // Fetch and populate PinCodeName if PinCodeId exists
    if (preloadedPinCodeId) {
        $.ajax({
            url: '/api/Company/GetPincodeName', // Endpoint to fetch PinCodeName
            type: 'GET',
            data: { pincodeId: preloadedPinCodeId },
            success: function (response) {
                if (response && response.pincodeName) {
                    $("#PinCodeId").val(response.pincodeName); // Populate input with name
                }
            },
            error: function () {
                console.error('Failed to fetch PinCodeName');
            }
        });
    }

    // Autocomplete logic remains the same
    $("#PinCodeId").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/api/Company/SearchPincode',
                data: {
                    term: request.term,
                    districtId: $("#SelectedDistrictId").val(),
                    stateId: $("#StateId").val(),
                    countryId: $("#CountryId").val()
                },
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.pincodeName,
                            value: item.pincodeName,
                            id: item.pincodeId
                        };
                    }));
                }
            });
        },
        minLength: 0,
        select: function (event, ui) {
            $("#PinCodeId").val(ui.item.label); // Set name in input
            $("#SelectedId").val(ui.item.id);  // Set ID in hidden field
            return false;
        }
    });

    // Clear the hidden field if the input is cleared manually
    $("#PinCodeId").on('input', function () {
        if (!$(this).val()) {
            $("#SelectedId").val('');
        }
    });
    $('input.auto-dropdown').on('focus', function () {
        $(this).select();
    });
});