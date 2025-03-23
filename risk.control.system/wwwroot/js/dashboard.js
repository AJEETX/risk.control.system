$(document).ready(function () {
    GetWeekly('Case', 'GetWeeklyClaim', 'container-claim');
    GetWeeklyTat('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    GetWeeklyPie('Case', 'GetWeeklyClaim', 'container-claim-pie');
    GetWeeklyAgencyPie('Agency ', 'GetAgentClaim', 'container-agency-pie');

    GetChart('Case', 'GetClaimChart', 'container-monthly-claim')

    $("#btnWeeklyReport").click(function () {
        GetWeekly('Case', 'GetWeeklyClaim', 'container-claim');
    })

    $("#btnMonthlyReport").click(function () {
        GetMonthly('Case', 'GetMonthlyClaim', 'container-claim');
    })
    $("#btnWeeklyPie").click(function () {
        GetWeeklyPie('Case', 'GetWeeklyClaim', 'container-claim-pie');
    })
    $("#btnMonthlyPie").click(function () {
        GetMonthlyPie('Case', 'GetMonthlyClaim', 'container-claim-pie');
    })
    $("#btnWeeklyTat").click(function () {
        GetWeeklyTat('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    })
    $("#btnMonthlyTat").click(function () {
        //GetMonthly('Case', 'GetClaimWeeklyTat', 'container-claim-tat');
    })

    $('.details-page').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('.details-page').attr('disabled', 'disabled');
        $('html').css('cursor', 'not-allowed');

        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});


function createCharts(container, txn, sum1, sum2, titleText, totalspent) {
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
        tooltip: {
            pointFormat: '{series.name}: <b>{point.y}</b> ({point.percentage:.1f}%)'
        },
        plotOptions: {
            pie: {
                allowPointSelect: true,
                cursor: 'pointer',
                dataLabels: {
                    enabled: true,
                    format: '<b>{point.name}</b>: {point.y}'
                }
            }
        },
        legend: {
            enabled: true
        },
        series: [
            {
                name: 'claims',
                type: 'pie',
                data: sum1,
                colorByPoint: true
            },
            {
                name: 'underwriting',
                type: 'pie',
                data: sum2,
                colorByPoint: true
            }
        ]
    });
}

function createChartColumn(container, txn, sum1, sum2, titleText, totalspent) {
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
            categories: [...new Set([...sum1.map(item => item[0]), ...sum2.map(item => item[0])])],
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
            enabled: true
        },
        tooltip: {
            shared: true
        },
        series: [
            {
                name: 'claims',
                type: 'column',
                data: sum1.map(item => item[1]), // Extract count1 values
                color: '#1f77b4' // Blue
            },
            {
                name: 'underwriting',
                type: 'column',
                data: sum2.map(item => item[1]), // Extract count2 values
                color: '#ff7f0e' // Orange
            }
        ]
    });
}
function createMonthChart(container, titleText, data1, data2, keys, total) {
    Highcharts.chart(container, {
        credits: {
            enabled: false
        },
        chart: {
            type: 'column' // Can be changed to 'line' if needed
        },
        title: {
            text: 'Total ' + titleText + ' Count ' + total,
            style: {
                fontSize: '1rem',
                fontFamily: 'Arial Narrow, sans-serif'
            }
        },
        xAxis: {
            categories: keys, // Month names or categories
            crosshair: true
        },
        yAxis: {
            min: 0,
            title: {
                text: 'Count'
            }
        },
        tooltip: {
            shared: true,
            pointFormat: '<span style="color:{series.color}">{series.name}</span>: <b>{point.y}</b><br/>'
        },
        legend: {
            enabled: true
        },
        series: [
            {
                name: 'claims',
                data: data1,
                color: '#1f77b4' // Blue
            },
            {
                name: 'underwriting',
                data: data2,
                color: '#ff7f0e' // Orange
            }
        ]
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
            if (result) {
                var keys = Object.keys(result);
                var weeklydata1 = [];
                var weeklydata2 = [];
                var totalspent = 0.0;

                Object.keys(result).forEach(key => {
                    var count1 = result[key].item1 || 0; // Extract count1
                    var count2 = result[key].item2 || 0; // Extract count2

                    weeklydata1.push([key, count1]);
                    weeklydata2.push([key, count2]);

                    totalspent += count1 + count2;
                });
                createMonthChart(container, title, weeklydata1, weeklydata2, keys, totalspent);
            }
        }
    })
}

function GetWeekly(title, url, container) {
    var titleMessage = "All Current " + title + ": Grouped by status";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata1 = [];
                var weeklydata2 = [];
                var totalspent = 0.0;

                Object.keys(result).forEach(key => {
                    var count1 = result[key].item1 || 0; // Extract count1
                    var count2 = result[key].item2 || 0; // Extract count2

                    weeklydata1.push([key, count1]);
                    weeklydata2.push([key, count2]);

                    totalspent += count1 + count2;
                });

                createChartColumn(container, title, weeklydata1, weeklydata2, titleMessage, totalspent);
            }
        }
    });
}
function GetWeeklyPie(title, url, container) {
    var titleMessage = "All Current " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
                var keys = Object.keys(result);
                var weeklydata1 = [];
                var weeklydata2 = [];
                var totalspent = 0.0;
                Object.keys(result).forEach(key => {
                    var count1 = result[key].item1 || 0; // Extract count1
                    var count2 = result[key].item2 || 0; // Extract count2

                    weeklydata1.push([key, count1]);
                    weeklydata2.push([key, count2]);

                    totalspent += count1 + count2;
                });

                createCharts(container, title, weeklydata1, weeklydata2, titleMessage, totalspent);
            }
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
            if (result) {
                var keys = Object.keys(result);
                var weeklydata1 = [];
                var weeklydata2 = [];
                var totalspent = 0.0;

                Object.keys(result).forEach(key => {
                    var count1 = result[key].item1 || 0; // Extract count1
                    var count2 = result[key].item2 || 0; // Extract count2

                    weeklydata1.push([key, count1]);
                    weeklydata2.push([key, count2]);

                    totalspent += count1 + count2;
                });

                createChartColumn(container, title, weeklydata1, weeklydata2, titleMessage, totalspent);
            }
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
            if (result) {
                var keys = Object.keys(result);
                var weeklydata1 = [];
                var weeklydata2 = [];
                var totalspent = 0.0;
                Object.keys(result).forEach(key => {
                    var count1 = result[key].item1 || 0; // Extract count1
                    var count2 = result[key].item2 || 0; // Extract count2

                    weeklydata1.push([key, count1]);
                    weeklydata2.push([key, count2]);

                    totalspent += count1 + count2;
                });

                createCharts(container, title, weeklydata1, weeklydata2, titleMessage, totalspent);
            }
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
            if (result) {
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
        }
    })
}

function createAgencyCharts(container, txn, sum, titleText, totalspent) {
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
            enabled: true, // Enables the legend
            layout: 'horizontal', // Horizontal legend
            align: 'center', // Center align
            verticalAlign: 'bottom' // Place legend at the bottom
        },
        tooltip: {
            pointFormat: 'Total ' + txn + ': Count <b>{point.y} </b>'
        },
        series: [{
            type: 'pie',
            name: txn + ' Count',
            data: sum,
            showInLegend: true // Ensure items appear in the legend
        }]
    });
}

function GetWeeklyAgencyPie(title, url, container) {
    var titleMessage = "All Current " + title + ":Count";
    $.ajax({
        type: "GET",
        url: "/Dashboard/" + url,
        contentType: "application/json",
        dataType: "json",
        success: function (result) {
            if (result) {
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

                createAgencyCharts(container, title, weeklydata, titleMessage, totalspent);
            }
        }
    })
}