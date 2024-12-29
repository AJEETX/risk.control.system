function loadInvestigationServices(obj) {
    var value = obj.value;
    if (value == '') {
        $('#InvestigationServiceTypeId').empty();
        $('#InvestigationServiceTypeId').append("<option value=''>--- SELECT ---</option>");
    }
    else {
        localStorage.setItem('lobId', value);
        $.get("/api/MasterData/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
            PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--- SELECT ---</option>");
        });
    }
}
function setInvestigationServices(obj) {
    localStorage.setItem('serviceId', obj.value);
}
function GetRemainingServicePinCode(showDefaultOption = true, vendorId, lineId) {
    var districtId = document.getElementById('SelectedDistrictId').value;

    var lobId = localStorage.getItem('lobId');

    var serviceId = localStorage.getItem('serviceId');

    $.get("/api/MasterData/GetPincodesByDistrictIdWithoutPreviousSelectedService", { districtId: districtId, vendorId: vendorId, lobId: lobId, serviceId: serviceId }, function (data) {
        PopulatePinCode("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function PopulatePinCode(dropDownId, list, option, showDefaultOption) {
    $(dropDownId).empty();
    $(dropDownId).val('');
    if (showDefaultOption)
        $(dropDownId).append(option)
    if (list && list.length > 0) {
        $.each(list, function (index, row) {
            $(dropDownId).append("<option value='" + row.pinCodeId + "'>" + row.name + " -- " + row.code + "</option>");
            $('#create-pincode').prop('disabled', false);
        });
    }
    else {
        $(dropDownId).append("<option value='-1'>NO - PINCODE - AVAILABLE</option>")
        $('#create-pincode').prop('disabled', true);
    }
}
function PopulateInvestigationServices(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.name + "</option>")
    });
}