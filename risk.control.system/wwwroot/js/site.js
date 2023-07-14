$(document).ready(function () {
    $("#datepicker").datepicker();

    if ($(".selected-case:checked").length) {
        $("#allocate-case").prop('disabled', false);
    }

    // When user checks a radio button, Enable submit button
    $(".selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocate-case").prop('disabled', false);
        }
    });

    $('#RawMessage').summernote({
        height: 300,                 // set editor height
        minHeight: null,             // set minimum height of editor
        maxHeight: null,             // set maximum height of editor
        focus: true                  // set focus to editable area after initializing summernote
    });

    $("#receipient-email").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: "/MasterData/GetUserBySearch",
                type: "POST",
                data: { search: request.term },
                success: function (data) {
                    response($.map(data, function (item) {
                        return { label: item, value: item };
                    }))
                }
            })
        },
        messages: {
            noResults: "",
            results: function (r) {
                return r;
            }
        },
        minLength: 3
    });

    $('.row-links').on('click', function () {
        let form = $('#message-detail');
        form.submit();
    });

    $('tbody tr').on('click', function () {
        let id = $(this).data('url');
        if (typeof id !== 'undefined') {
            window.location.href = id;
        }
    });

    // delete messages
    $('#delete-messages').on('click', function () {
        let ids = [];
        let form = $('#listForm');
        let checkboxArray = document.getElementsByName('ids');

        // check if checkbox is checked
        for (let i = 0; i < checkboxArray.length; i++) {
            if (checkboxArray[i].checked)
                ids.push(checkboxArray[i].value);
        }

        // submit form
        if (ids.length > 0) {
            if (confirm("Are you sure you want to delete this item(s)?")) {
                form.submit();
            }
        }
    });

    $('#delete-message').on('click', function () {
        $('#deleteForm').submit();
    });

    // Attach the call to toggleChecked to the
    // click event of the global checkbox:
    $("#checkall").click(function () {
        var status = $("#checkall").prop('checked');
        $('#manage-vendors').prop('disabled', !status)
        toggleChecked(status);
    });

    $("input.vendors").click(function () {
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
    GetWeekly('Claim', 'GetWeeklyClaim', 'container-claim');
    //GetWeekly('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    GetWeeklyPie('Claim', 'GetWeeklyClaim', 'container-claim-pie');

    GetChart('Claim', 'GetClaimChart', 'container-monthly-claim')

    $("#btnWeeklyReport").click(function () {
        GetWeekly('Claim', 'GetWeeklyClaim', 'container-claim');
    })

    $("#btnMonthlyReport").click(function () {
        GetMonthly('Claim', 'GetMonthlyClaim', 'container-claim');
    })
    $("#btnWeeklyPie").click(function () {
        GetWeeklyPie('Claim', 'GetWeeklyClaim', 'container-claim-pie');
    })
    $("#btnMonthlyPie").click(function () {
        GetMonthlyPie('Claim', 'GetMonthlyClaim', 'container-claim-pie');
    })
    $("#btnWeeklyTat").click(function () {
        //GetWeekly('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    })
    $("#btnMonthlyTat").click(function () {
        //GetMonthly('Claim', 'GetClaimWeeklyTat', 'container-claim-tat');
    })
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
            hasAnyCheckboxChecked = true;
        }
    });
    return hasAnyCheckboxChecked;
}
function loadState(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/MasterData/GetStatesByCountryId", { countryId: value }, function (data) {
        PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option>--SELECT STATE--</option>", "<option>--SELECT DISTRICT--</option>", "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}
function loadDistrict(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/MasterData/GetDistrictByStateId", { stateId: value }, function (data) {
        PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option>--SELECT PINCODE--</option>", "<option>--SELECT DISTRICT--</option>", showDefaultOption);
    });
}
function loadPinCode(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/MasterData/GetPinCodesByDistrictId", { districtId: value }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function loadSubStatus(obj) {
    var value = obj.value;
    $.post("/InvestigationCaseSubStatus/GetSubstatusBystatusId", { InvestigationCaseStatusId: value }, function (data) {
        PopulateSubStatus("#InvestigationCaseSubStatusId", data, "<option>--SELECT SUB STATUS--</option>");
    });
}

function PopulateSubStatus(dropDownId, list, option) {
    $(dropDownId).empty();
    $(dropDownId).append(option)
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.code + "</option>")
    });
}
let lobObj;
let investigationServiceObj;

function loadInvestigationServices(obj) {
    var value = obj.value;
    lobObj = value;
    localStorage.setItem('lobId', value);
    $.post("/VendorService/GetInvestigationServicesByLineOfBusinessId", { LineOfBusinessId: value }, function (data) {
        PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--SELECT TYPE OF INVESTIGATION--</option>");
    });
}

function setInvestigationServices(obj) {
    localStorage.setItem('serviceId', obj.value);
    investigationServiceObj = obj.value;
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
    if (showDefaultOption) {
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
function readURL(input) {
    var url = input.value;
    var ext = url.substring(url.lastIndexOf('.') + 1).toLowerCase();
    if (input.files && input.files[0] && (ext == "gif" || ext == "png" || ext == "jpeg" || ext == "jpg" || ext == "csv")) {
        var reader = new FileReader();

        reader.onload = function (e) {
            $('#img').attr('src', e.target.result);
        }

        reader.readAsDataURL(input.files[0]);
    } else {
        $('#img').attr('src', '/img/no-image.png');
    }
}

function loadRemainingPinCode(obj, showDefaultOption = true, caseId) {
    var value = obj.value;
    $.post("/MasterData/GetPincodesByDistrictIdWithoutPreviousSelected", { districtId: value, caseId: caseId }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}
function loadRemainingServicePinCode(obj, showDefaultOption = true, vendorId, lineId) {
    var value = obj.value;

    var lobId = localStorage.getItem('lobId');

    var serviceId = localStorage.getItem('serviceId');

    $.post("/MasterData/GetPincodesByDistrictIdWithoutPreviousSelectedService", { districtId: value, vendorId: vendorId, lobId: lobId, serviceId: serviceId }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--SELECT PINCODE--</option>", showDefaultOption);
    });
}

function createCharts(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        chart: {
            type: 'pie'
        },
        title: {
            text: titleText + ' ' + totalspent,
            style: {
                fontSize: '.9rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            type: 'category',
            labels: {
                rotation: -45,
                style: {
                    fontSize: '12px',
                    fontFamily: 'Arial Narrow, sans-serif'
                }
            }
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Count'
            }
        },
        legend: {
            enabled: false
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Count <b>{point.y:.2f} </b>'
        },
        series: [{
            type: 'pie',
            data: sum,
        }]
    });
}
function createChartColumn(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        chart: {
            type: 'column'
        },
        title: {
            text: titleText + ' ' + totalspent,
            style: {
                fontSize: '.9rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            type: 'category',
            labels: {
                rotation: -45,
                style: {
                    fontSize: '12px',
                    fontFamily: 'Arial Narrow, sans-serif'
                }
            }
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Count'
            }
        },
        legend: {
            enabled: false
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Count <b>{point.y:.2f} </b>'
        },
        series: [{
            type: 'column',
            data: sum,
        }]
    });
}
function createMonthChart(container, titleText, data, keys, total) {
    Highcharts.chart(container, {
        chart: {
            marginRight: 0
        },
        title: {
            text: 'Total ' + titleText + ' Count ' + total,
            style: {
                fontSize: '1rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        legend: {
            enabled: false
        },
        xAxis: {
            categories: keys
        },
        yAxis: {
            min: 0,
            title: {
                text: ' Count'
            }
        },
        series: [{
            data: data,
            color: 'green'
        }]
    });
}

function GetChart(title, url, container) {
    var titleMessage = "Last 12 month " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            var keys = Object.keys(result);
            var weeklydata = new Array();
            var totalspent = 0.0;
            for (var i = 0; i < keys.length; i++) {
                var arrL = new Array();
                arrL.push(keys[i]);
                arrL.push(result[keys[i]]);
                totalspent += result[keys[i]];
                weeklydata.push(arrL);
            }
            createMonthChart(container, title, weeklydata, keys, totalspent);
        }
    })
}

function GetWeekly(title, url, container) {
    var titleMessage = "Last 4 week " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            var keys = Object.keys(result);
            var weeklydata = new Array();
            var totalspent = 0.0;
            for (var i = 0; i < keys.length; i++) {
                var arrL = new Array();
                arrL.push(keys[i]);
                arrL.push(result[keys[i]]);
                totalspent += result[keys[i]];
                weeklydata.push(arrL);
            }
            createChartColumn(container, title, weeklydata, titleMessage, totalspent);
        }
    })
}
function GetWeeklyPie(title, url, container) {
    var titleMessage = "Last 4 week " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            var keys = Object.keys(result);
            var weeklydata = new Array();
            var totalspent = 0.0;
            for (var i = 0; i < keys.length; i++) {
                var arrL = new Array();
                arrL.push(keys[i]);
                arrL.push(result[keys[i]]);
                totalspent += result[keys[i]];
                weeklydata.push(arrL);
            }
            createCharts(container, title, weeklydata, titleMessage, totalspent);
        }
    })
}

function GetMonthly(title, url, container) {
    var titleMessage = "Last 6 month " + title + "Count";

    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            var keys = Object.keys(result);
            var monthlydata = new Array();
            var totalspent = 0.0;
            for (var i = 0; i < keys.length; i++) {
                var arrL = new Array();
                arrL.push(keys[i]);
                arrL.push(result[keys[i]]);
                totalspent += result[keys[i]];
                monthlydata.push(arrL);
            }
            createChartColumn(container, title, monthlydata, titleMessage, totalspent);
        }
    })
}
function GetMonthlyPie(title, url, container) {
    var titleMessage = "Last 6 month " + title + "Count";

    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            var keys = Object.keys(result);
            var monthlydata = new Array();
            var totalspent = 0.0;
            for (var i = 0; i < keys.length; i++) {
                var arrL = new Array();
                arrL.push(keys[i]);
                arrL.push(result[keys[i]]);
                totalspent += result[keys[i]];
                monthlydata.push(arrL);
            }
            createCharts(container, title, monthlydata, titleMessage, totalspent);
        }
    })
}