$(document).ready(function () {
    $('a#back').attr("href", "/Dashboard/Index");
    $('a.create').attr("href", "/IpAddress/Create");
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/MasterData/GetWhitelist',
            dataSrc: ''
        },
        order: [[3, 'asc']],
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.ipAddressId + '" title="' + row.ipAddressId + ' class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
       
            { "data": "address" },
            { "data": "updatedBy" },
            { "data": "updated" },
     
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",
                columnClass: 'medium',
                content: "Are you sure to add?",
                icon: 'fas fa-sitemap',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: " Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $('#create-form').submit();
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

