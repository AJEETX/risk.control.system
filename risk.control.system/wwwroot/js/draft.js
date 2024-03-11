$(document).ready(function () {
    $('#postedFile').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadFileButton');
        var uploadType = $('#uploadtype').val();
        val.endsWith('.zip') && (uploadType == "0" || uploadType == "1") ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
    });

    $('#uploadtype').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadFileButton');
        var uploadType = $('#postedFile').val();
        (val == "0" || val == "1") && uploadType.endsWith('.zip') ? fbtn.removeAttr("disabled") : fbtn.attr('disabled', 'disabled');
    });

    $('#view-type a').on('click', function () {
        var id = this.id;
        if (this.id == 'map-type') {
            $('#checkboxes').css('display', 'none');
            $('#maps').css('display', 'block');
            $('#map-type').css('display', 'none');
            $('#list-type').css('display', 'block');
        }
        else {
            $('#checkboxes').css('display', 'block');
            $('#maps').css('display', 'none');
            $('#map-type').css('display', 'block');
            $('#list-type').css('display', 'none');
        }
    });

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/CompanyDraftClaims/GetAssign',
            dataSrc: ''
        },
        columnDefs: [{
            'targets': 0,
            'searchable': false,
            'orderable': false,
            'className': 'dt-body-center',
            'render': function (data, type, full, meta) {
                return '<input type="checkbox" name="id[]" value="' + $('<div/>').text(data).html() + '">';
            }
        }],
        order: [[12, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,

        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from
            data file source */
            {
                "sDefaultContent": "<i class='fa fa-file-alt'></i>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input class="vendors" name="claims" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  />';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "policyNum", "bSortable": false },
            {
                "data": "amount"
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "name" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.beneficiaryName + '" title="' + row.beneficiaryName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "beneficiaryName" },
            { "data": "serviceType" },
            { "data": "service" },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="edit' + row.id + '" onclick="showedit(`' + row.id + '`)" href="Details?Id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pencil-alt"></i> Edit</a>&nbsp;'
                    buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)" href="/InsurancePolicy/Delete?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });
    // Handle click on "Select all" control
    $('#checkall').on('click', function () {
        // Get all rows with search applied
        var rows = table.rows({ 'search': 'applied' }).nodes();
        // Check/uncheck checkboxes for all rows in the table
        $('input[type="checkbox"]', rows).prop('checked', this.checked);
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#customerTable tbody').on('change', 'input[type="checkbox"]', function () {
        // If checkbox is not checked
        if (!this.checked) {
            var el = $('#checkall').get(0);
            // If "Select all" control is checked and has 'indeterminate' property
            if (el && el.checked && ('indeterminate' in el)) {
                // Set visual state of "Select all" control
                // as 'indeterminate'
                el.indeterminate = true;
            }
        }
    });
    let askConfirmation = false;
    // Handle form submission event
    $('#checkboxes').on('submit', function (e) {
        var form = this;

        // Iterate over all checkboxes in the table
        table.$('input[type="checkbox"]').each(function () {
            // If checkbox doesn't exist in DOM
            if (!$.contains(document, this)) {
                // If checkbox is checked
                if (this.checked) {
                    // Create a hidden element
                    $(form).append(
                        $('<input>')
                            .attr('type', 'hidden')
                            .attr('name', this.name)
                            .val(this.value)
                    );
                }
            }
        });

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);

        if (!anyChecked) {
            e.preventDefault();
            $.alert({
                title: "Claim Assignment !!!",
                content: "Please select claim(s) to assign?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "SELECT",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Assignment",
                content: "Are you sure to assign?",
                icon: 'fas fa-external-link-alt',
                columnClass: 'medium',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Assign",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#managevendors').attr('disabled', 'disabled');
                            $('#managevendors').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign");

                            $('#checkboxes').submit();
                            var nodes = document.getElementById("fullpage").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    });

    $('#UploadFileButton').on('click', function (event) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $(this).attr('disabled', 'disabled');
        $(this).html("<i class='fas fa-sync fa-spin'></i> Upload");

        $('#upload-claims').submit();
        $('html *').css('cursor', 'not-allowed');
        $('html a *, html button *').attr('disabled', 'disabled');
        $('html a *, html button *').css('pointer-events', 'none')

        var nodes = document.getElementById("body").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
    initMap("/api/CompanyDraftClaims/GetAssignMap");

});

function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn *').attr('disabled', 'disabled');
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn *').attr('disabled', 'disabled');
    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}


function Delete(userId, status) {
    $.confirm({
        title: 'Change Status!',
        content: 'Do you want to Change Status!',
        buttons: {
            confirm: function () {
                $.ajax({
                    url: "/Administration/User/ChangeUserStatus",
                    type: "POST",
                    data: { UserId: userId, Status: status },
                    success: function (data, textStatus, xhr) {
                        if (data.Result == "success") {
                            location.reload();
                        }
                        if (data.Result == "failed") {
                            $.alert('Something Went Wrong');
                        }
                    },
                    error: function (xhr, status, err) {
                        if (xhr.status == 401) {
                            alert('Error');
                            window.location.href = "/Portal/Logout";
                        }
                        if (xhr.status == 500) {
                            alert('Error');
                            window.location.href = "/Portal/Logout";
                        }
                    }
                });
            },
            cancel: function () {
                $.alert('Canceled!');
            }
        }
    });
}