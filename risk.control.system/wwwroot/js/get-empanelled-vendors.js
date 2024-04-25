$(function () {

    //var table = $("#customerTable").DataTable({
    //    ajax: {
    //        url: '/api/Agency/GetEmpannelled',
    //        dataSrc: ''
    //    },
    //    columnDefs: [{
    //        'targets': 0,
    //        'searchable': false,
    //        'orderable': false,
    //        'className': 'dt-body-center',
    //        'render': function (data, type, full, meta) {
    //            return '<input type="checkbox" name="selectedcase[]" value="' + $('<div/>').text(data).html() + '">';
    //        }
    //    }],
    //    order: [[11, 'asc']],
    //    fixedHeader: true,
    //    processing: true,
    //    paging: true,
    //    language: {
    //        loadingRecords: '&nbsp;',
    //        processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
    //    },
    //    columns: [
    //        /* Name of the keys from
    //        data file source */
    //        {
    //            "sDefaultContent": "",
    //            "bSortable": false,
    //            "mRender": function (data, type, row) {
    //                var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '"  />';
    //                return img;
    //            }
    //        },
    //        {
    //            "sDefaultContent": "",
    //            "bSortable": false,
    //            "mRender": function (data, type, row) {
    //                var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '"class="doc-profile-image" data-toggle="tooltip"/>';
    //                return img;
    //            }
    //        },
    //        { "data": "policyNum", "bSortable": false },
    //        {
    //            "data": "amount"
    //        },
    //        {
    //            "sDefaultContent": "",
    //            "bSortable": false,
    //            "mRender": function (data, type, row) {
    //                var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
    //                return img;
    //            }
    //        },
    //        { "data": "name" },
    //        {
    //            "sDefaultContent": "",
    //            "bSortable": false,
    //            "mRender": function (data, type, row) {
    //                var img = '<img alt="' + row.beneficiaryName + '" title="' + row.beneficiaryName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" data-toggle="tooltip"/>';
    //                return img;
    //            }
    //        },
    //        { "data": "beneficiaryName" },
    //        { "data": "serviceType" },
    //        { "data": "service" },
    //        {
    //            "data": "pincode",
    //            "mRender": function (data, type, row) {
    //                return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
    //            }
    //        },
    //        { "data": "location" },
    //        { "data": "created" },
    //        { "data": "timePending" },
    //        {
    //            "sDefaultContent": "",
    //            "bSortable": false,
    //            "mRender": function (data, type, row) {
    //                var buttons = "";
    //                buttons += '<a id="edit' + row.id + '" onclick="showedit(`' + row.id + '`)" href="DetailsManual?Id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pencil-alt"></i> Edit</a>&nbsp;'
    //                buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)" href="/InsurancePolicy/DeleteManual?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
    //                return buttons;
    //            }
    //        }
    //    ],
    //    error: function (xhr, status, error) { alert('err ' + error) }
    //});

    //$('#customerTable').on('draw.dt', function () {
    //    $('[data-toggle="tooltip"]').tooltip();
    //});


    $('#empanelled.btn.btn-info').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('.btn.btn-info').attr('disabled', 'disabled');
        $(this).html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Details");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });


    $("img.rating").mouseover(function () {
        giveRating($(this), "FilledStar.jpeg");
        $(this).css("cursor", "pointer");
    });

    $("img.rating").mouseout(function () {
        giveRating($(this), "StarFade.gif");
        refilRating($(this));
    });

    $("img.rating").click(function (e) {
        $(this).css('color', 'red');
        var url = "/Vendors/PostRating?rating=" + parseInt($(this).attr("id")) + "&mid=" + $(this).attr("vendorId");
        $.post(url, null, function (data) {
            $(e.currentTarget).closest('tr').find('span.result').text(data).css('color', 'red');
            $("#result").text(data);
        });
    });

    $("#datatable > tbody  > tr").each(function () {
        var av = $(this).find("span.avr").text();

        if (av != "" || av != null) {
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
        }
    });
    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm ASSIGN & RE",
                content: "Are you sure ?",
    
                icon: 'fas fa-external-link-alt',
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "ASSIGN & RE",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#allocate-case').attr('disabled', 'disabled');
                            $('#allocate-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ASSIGN & RE");

                            $('#radioButtons').submit();
                            var nodes = document.getElementById("article").getElementsByTagName('*');
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
    })
});

function giveRating(img, image) {
    img.attr("src", "/Images/" + image)
        .prevAll("img.rating").attr("src", "/Images/" + image);
}
function refilRating(img1) {
    var rt = $(img1).closest('tr').find("span.avr").text();
    var img = $(img1).closest('tr').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
}