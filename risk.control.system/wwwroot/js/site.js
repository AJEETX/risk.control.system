$(document).ready(function () {

    /** add active class and stay opened when selected */
    var url = window.location;
    const allLinks = document.querySelectorAll('.nav-item a');
    const currentLink = [...allLinks].filter(e => {
        return e.href == url;
    });

    if (currentLink.length > 0) { //this filter because some links are not from menu
        currentLink[0].classList.add("active");
        if (currentLink[0].closest(".nav-treeview")) {
            currentLink[0].closest(".nav-treeview").style.display = "block";
        }
    }
    // Attach the call to toggleChecked to the
    // click event of the global checkbox:
    $("#checkall").click(function () {
        var status = $("#checkall").prop('checked');
        $('#manage-vendors').prop('disabled', !status)
        toggleChecked(status);
    });

    $("input.vendors").click(function () {
        //var status = $(this).prop('checked');

        //$(this).prop('checked', status);
        //$('#manage-vendors').prop('disabled', !status);

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        var allChecked = checkIfAllChecked(checkboxes);
        $('#checkall').prop('checked', allChecked);
        $('#manage-vendors').prop('disabled', !anyChecked)
    });

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

function checkIfAllChecked(elements) {
    var totalElmentCount = elements.length;
    var totalCheckedElements = elements.filter(":checked").length;
    return (totalElmentCount == totalCheckedElements)
}

function checkIfAnyChecked(elements) {
    var hasAnyCheckboxChecked = false;

    $.each(elements, function (index, element) {
        if (element.checked === true) {
            hasAnyCheckboxChecked= true;
        }
    });
    return hasAnyCheckboxChecked;
}
function loadState(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/User/GetStatesByCountryId", { countryId: value }, function (data) {
        PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option>--SELECT STATE--</option>", "<option>--SELECT DISTRICT--</option>", "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}
function loadDistrict(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/User/GetDistrictByStateId", { stateId: value }, function (data) {
        PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option>--SELECT PINCODE--</option>", "<option>--SELECT DISTRICT--</option>", showDefaultOption);
    });
}
function loadPinCode(obj, showDefaultOption= true) {
    var value = obj.value;
    $.post("/User/GetPinCodesByDistrictId", { districtId: value }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function loadInvestigationServices(obj) {
    var value = obj.value;
    $.post("/VendorService/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
        PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--SELECT TYPE OF INVESTIGATION--</option>");
    });
}
function PopulateInvestigationServices(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.code + "</option>")
    });
}
function PopulateDistrictDropDown(pinCodedropDownId, districtDropdownId, list, pincodeOption, districtOption, showDefaultOption) {
    $(pinCodedropDownId).empty();
    if (showDefaultOption){
        $(pinCodedropDownId).append(pincodeOption)
    }

    $(districtDropdownId).empty();
    $(districtDropdownId).append(districtOption)

    $.each(list, function (index, row) {
        $(districtDropdownId).append("<option value='" + row.districtId + "'>" + row.name + "</option>")
    });
}
function PopulatePinCodeDropDown(dropDownId, list, option, showDefaultOption) {
    $(dropDownId).empty();
    if (showDefaultOption)
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + " -- " + row.code + "</option>")
    });
}
function PopulateStateDropDown(pinCodedropDownId, districtDropDownId, stateDropDownId, list, stateOption, districtOption, pincodeOption, showDefaultOption) {
    $(stateDropDownId).empty();
    $(districtDropDownId).empty();
    $(pinCodedropDownId).empty();

    $(stateDropDownId).append(stateOption);
    $(districtDropDownId).append(districtOption);
    if (showDefaultOption) {
        $(pinCodedropDownId).append(pincodeOption);
    }

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
