$(document).ready(function () {
    $('#checkboxes').on('input change', function() {
        var ele = $(this).val();
        if( ele != '') {
            $('#broadcast').prop('disabled', true);
        } else {
            $('#broadcast').prop('disabled', false);
        }
    });
    // Attach the call to toggleChecked to the
    // click event of the global checkbox:
    $("#checkall").click(function () {
        var status = $("#checkall").prop('checked');
        $('#broadcast').prop('disabled', !status)
        toggleChecked(status);
    });

    $('#CountryId').change(function(){
        loadState($(this));
    });
    $('#StateId').change(function(){
        loadDistrict($(this));
    });   
    $('#DistrictId').change(function () {
        loadPinCode($(this));
    });   
    //$("select").each(function () {
    //    if ($(this).find("option").length <= 1) {
    //        $(this).attr("disabled", "disabled");
    //    }
    //});
    $("#btnDeleteImage").click(function () {
        var id = $(this).attr("data-id");
        $.ajax({
            url: '/User/DeleteImage/' + id,
            type: "POST",
            async: true,
            success: function (data) {
                if (data.succeeded) {
                    $("#delete-image-main").hide();
                    $("#ProfilePictureUrl").val("");
                }
                else {
                   // toastr.error(data.message);
                }
            },
            beforeSend: function () {
                $(this).attr("disabled", true);
                $(this).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
            },
            complete: function () {
                $(this).html('Delete Image');
            },
        });
    });
});
function loadState(obj) {
    var value = obj.value;
    $.post("/User/GetStatesByCountryId", { countryId: value }, function (data) {
        PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option>--SELECT STATE--</option>", "<option>--SELECT DISTRICT--</option>", "<option>--SELECT PINCODE--</option>");
    });
}
function loadDistrict(obj) {
    var value = obj.value;
    $.post("/User/GetDistrictByStateId", { stateId: value }, function (data) {
        PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option>--SELECT DISTRICT--</option>", "<option>--SELECT PINCODE--</option>");
    });
}
function loadPinCode(obj) {
    var value = obj.value;
    $.post("/User/GetPinCodesByDistrictId", { districtId: value }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", "<option>--SELECT DISTRICT--</option>");
    });
}
function PopulateDistrictDropDown(pinCodedropDownId, districtDropdownId, list, option, districtOption) {
    $(pinCodedropDownId).empty();
    $(pinCodedropDownId).append(option)

    $(districtDropdownId).empty();
    $(districtDropdownId).append(districtOption)

    $.each(list, function (index, row) {
        $(districtDropdownId).append("<option value='" + row.districtId + "'>" + row.name + "</option>")
    });
}
function PopulatePinCodeDropDown(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + "</option>")
    });
}
function PopulateStateDropDown(pinCodedropDownId, districtDropDownId, stateDropDownId, list, option, districtOption, pincodeOption) {
    $(stateDropDownId).empty();
    $(districtDropDownId).empty();
    $(pinCodedropDownId).empty();

    $(stateDropDownId).append(option);
    $(districtDropDownId).append(districtOption);
    $(pinCodedropDownId).append(pincodeOption);

    $.each(list, function (index, row) {
        $(stateDropDownId).append("<option value='" + row.stateId + "'>" + row.name + "</option>")
    });
}
function toggleChecked(status) {
    $("#checkboxes input").each(function () {
        // Set the checked status of each to match the 
        // checked status of the check all checkbox:
        $(this).prop("checked", status);        
    });
}
