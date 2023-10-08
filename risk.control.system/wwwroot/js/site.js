$(document).ready(function () {
    $('#datatable thead th').css('background-color', '#e9ecef')
    var datatable = $('#datatable').dataTable({
        processing: true,
        ordering: true,
        paging: true,
        searching: true,
        //'fnDrawCallback': function (oSettings) {
        //    $('.dataTables_filter').each(function () {
        //        $(this).prepend('<button class="btn btn-success mr-xs pull-right" type="button"><i class="fas fa-plus"></i>  Add</button>');
        //    });
        //}
    });

    $("#datepicker").datepicker({ maxDate: '0' });

    if ($(".selected-case:checked").length) {
        $("#allocate-case").prop('disabled', false);
    }
    else {
        $("#allocate-case").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $(".selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocate-case").prop('disabled', false);
        }
        else {
            $("#allocate-case").prop('disabled', true);
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

    $('.investigation-Image').click(function () {
        $.confirm({
            type: 'grey',
            closeIcon: true,
            columnClass: 'medium',
            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetInvestigationData?id=' + $('#beneficiaryId').val() + '&claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('Photo with Beneficiary: <img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.location + '" /> ');
                    self.setContentAppend('<br><img id="agentLocation" class="img-fluid investigation-actual-image" src="' + response.latLong + '" /> ');
                    self.setContentAppend('<br>OCR Photo: <img id="agentOcrPicture" class="img-fluid investigation-actual-image" src="' + response.ocrData + '" /> ');
                    self.setContentAppend('<br><img id="ocrLocation" class="img-fluid investigation-actual-image" src="' + response.ocrLatLong + '" /> ');
                    self.setContentAppend('<br>OCR Data : ');
                    self.setContentAppend('<br>' + response.qrData);
                    self.setTitle('<i class="fas fa-mobile-alt"></i> ' + response.title);
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

    $('#policy-detail').click(function () {
        $.confirm({
            columnClass: 'medium',
            title: "Policy detail",
            closeIcon: true,
            columnClass: 'medium',
            type: 'grey',
            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    'type': 'GET',
                    'url': '/api/ClaimsInvestigation/GetPolicyDetail?id=' + $('#policyDetailId').val(),
                    'dataType': 'json',
                    'success': function (response) {
                        console.log(response);
                        self.setTitle('<i class="far fa-file-powerpoint"></i> Policy detail ');
                        self.setContent('Policy Doc: <img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.document + '" /> ');
                        self.setContentAppend('<br>Policy #: ' + response.contractNumber);
                        self.setContentAppend('<br>Claim type: ' + response.claimType);
                        self.setContentAppend('<br>Issue date: ' + response.contractIssueDate);
                        self.setContentAppend('<br>Incident date: ' + response.dateOfIncident);
                        self.setContentAppend('<br>Amount : <i class="fas fa-rupee-sign"></i>' + response.sumAssuredValue);
                        self.setContentAppend('<br>Service : ' + response.investigationServiceType);
                        self.setContentAppend('<br>Reason : ' + response.caseEnabler);
                        self.setContentAppend('<br>Cause : ' + response.causeOfLoss);
                    }
                }, function () {
                    //This function is for unhover.
                });
            }
        })
    });

    $('#customer-detail').click(function () {
        $.confirm({
            columnClass: 'medium',
            title: "Customer detail",
            icon: 'fa fa-user-plus',
            closeIcon: true,
            columnClass: 'medium',
            type: 'grey',
            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetCustomerDetail?id=' + $('#customerDetailId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('Photo: <img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.customer + '" /> ');
                    self.setContentAppend('<br>Customer: ' + response.customerName);
                    self.setContentAppend('<br>Occupation #: ' + response.occupation);
                    self.setContentAppend('<br>Income #: ' + response.income);
                    self.setContentAppend('<br>Education #: ' + response.education);
                    self.setContentAppend('<br>Address #: ' + response.address);
                    self.setContentAppend('<br>Phone #: ' + response.contactNumber);
                    self.setTitle('Customer detail');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })

    $('#beneficiary-detail').click(function () {
        $.confirm({
            columnClass: 'medium',
            title: "Beneficiary detail",
            icon: 'fas fa-user-tie',
            closeIcon: true,
            columnClass: 'medium',
            type: 'grey',
            buttons: {
                confirm: {
                    text: "Ok",
                    btnClass: 'btn-secondary',
                    action: function () {
                        askConfirmation = false;
                    }
                }
            },
            content: function () {
                var self = this;
                return $.ajax({
                    url: '/api/ClaimsInvestigation/GetBeneficiaryDetail?id=' + $('#beneficiaryId').val() + '&claimId=' + $('#claimId').val(),
                    dataType: 'json',
                    method: 'get'
                }).done(function (response) {
                    self.setContent('Photo: <img id="agentLocationPicture" class="img-fluid investigation-actual-image" src="' + response.beneficiary + '" /> ');
                    self.setContentAppend('<br>Beneficiary: ' + response.beneficiaryName);
                    self.setContentAppend('<br>Relation #: ' + response.beneficiaryRelation);
                    self.setContentAppend('<br>Phone #: ' + response.contactNumber);
                    self.setContentAppend('<br>Income #: ' + response.income);
                    self.setContentAppend('<br>Address #: ' + response.address);
                    self.setTitle(' Beneficiary detail');
                }).fail(function () {
                    self.setContent('Something went wrong.');
                });
            }
        })
    })
    $('#policy-comments').click(function () {
        $.confirm({
            title: 'Policy Note!!!',
            icon: 'far fa-file-powerpoint',
            content: '' +
                '<form action="" class="formName">' +
                '<div class="form-group">' +
                '<label>Enter note on Policy</label>' +
                '<input type="text" placeholder="Enter note" class="name form-control" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Add Note',
                    btnClass: 'btn-blue',
                    action: function () {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert('Provide Policy note!!!');
                            return false;
                        }
                        $.alert('Policy note is ' + name);
                    }
                },
                cancel: function () {
                    //close
                },
            },
            onContentReady: function () {
                // bind to events
                var jc = this;
                this.$content.find('form').on('submit', function (e) {
                    // if the user submits the form by pressing enter in the field.
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click'); // reference the button and click it
                });
            }
        });
    })

    $('#customer-comments').click(function () {
        $.confirm({
            title: 'Customer Note!!!',
            icon: 'fa fa-user-plus',
            content: '' +
                '<form action="" class="formName">' +
                '<div class="form-group">' +
                '<label>Enter note on Customer</label>' +
                '<input type="text" placeholder="Enter note" class="name form-control" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Add Note',
                    btnClass: 'btn-blue',
                    action: function () {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert('Provide Customer note!!!');
                            return false;
                        }
                        $.alert('Customer note is ' + name);
                    }
                },
                cancel: function () {
                    //close
                },
            },
            onContentReady: function () {
                // bind to events
                var jc = this;
                this.$content.find('form').on('submit', function (e) {
                    // if the user submits the form by pressing enter in the field.
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click'); // reference the button and click it
                });
            }
        });
    })

    $('#beneficiary-comments').click(function () {
        $.confirm({
            title: 'Beneficiary Note!!!',
            icon: 'fas fa-user-tie',
            content: '' +
                '<form action="" class="formName">' +
                '<div class="form-group">' +
                '<label>Enter note about Beneficiary</label>' +
                '<input type="text" placeholder="Enter note" class="name form-control" required />' +
                '</div>' +
                '</form>',
            buttons: {
                formSubmit: {
                    text: 'Add Note',
                    btnClass: 'btn-blue',
                    action: function () {
                        var name = this.$content.find('.name').val();
                        if (!name) {
                            $.alert('Provide Beneficiary note!!!');
                            return false;
                        }
                        $.alert('Beneficiary note is ' + name);
                    }
                },
                cancel: function () {
                    //close
                },
            },
            onContentReady: function () {
                // bind to events
                var jc = this;
                this.$content.find('form').on('submit', function (e) {
                    // if the user submits the form by pressing enter in the field.
                    e.preventDefault();
                    jc.$$formSubmit.trigger('click'); // reference the button and click it
                });
            }
        });
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
        PopulateStateDropDown("#PinCodeId", "#DistrictId", "#StateId", data, "<option>--- SELECT ---</option>", "<option>--- SELECT ---</option>", "<option>--- SELECT ---</option>", showDefaultOption);
    });
}
function loadDistrict(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/MasterData/GetDistrictByStateId", { stateId: value }, function (data) {
        PopulateDistrictDropDown("#PinCodeId", "#DistrictId", data, "<option>--- SELECT ---</option>", "<option>--- SELECT ---</option>", showDefaultOption);
    });
}
function loadPinCode(obj, showDefaultOption = true) {
    var value = obj.value;
    $.post("/MasterData/GetPinCodesByDistrictId", { districtId: value }, function (data) {
        PopulatePinCodeDropDown("#PinCodeId", data, "<option>--- SELECT ---</option>", showDefaultOption);
    });
}

function enableSubmitButton(obj, showDefaultOption = true) {
    var value = obj.value;
    $('#create-pincode').prop('disabled', false);
}

function loadSubStatus(obj) {
    var value = obj.value;
    $.post("/InvestigationCaseSubStatus/GetSubstatusBystatusId", { InvestigationCaseStatusId: value }, function (data) {
        PopulateSubStatus("#InvestigationCaseSubStatusId", data, "<option>--- SELECT ---</option>");
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
        PopulateInvestigationServices("#InvestigationServiceTypeId", data, "<option>--- SELECT ---</option>");
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
    $(pinCodedropDownId).append(pincodeOption)

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
        credits: {
            enabled: false
        },
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
            pointFormat: 'Total ' + txn + ': Count <b>{point.y} </b>'
        },
        series: [{
            type: 'pie',
            data: sum,
        }]
    });
}
function createChartColumn(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
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
            pointFormat: 'Total ' + txn + ': Count <b>{point.y} </b>'
        },
        series: [{
            type: 'column',
            data: sum,
        }]
    });
}
function createMonthChart(container, titleText, data, keys, total) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
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
    var titleMessage = "All Current " + title + ":Grouped by status";
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
    var titleMessage = "All Current " + title + ":Count";
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
    var titleMessage = "All Current " + title + "Count by status";

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
    var titleMessage = "All Current " + title + "Count by status";

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

function createChartTat(container, txn, sum, titleText, totalspent) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
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
            categories: ['0 Day', '1 Day', '2 Day', '3 Day', '4 Day', '5 plus Day']
        },
        yAxis: {
            min: 0,
            title: {
                text: txn + ' Status changes '
            }
        },
        legend: {
            enabled: true
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Status changes <b>{point.y} </b>'
        },
        series: sum
    });
}
function GetWeeklyTat(title, url, container) {
    var titleMessage = "All Current " + title + ":Status changes";
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
            createChartTat(container, title, result.tatDetails, titleMessage, result.count);
        }
    })
}