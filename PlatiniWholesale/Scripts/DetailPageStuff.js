$(function () {
    $("input[type='text'].qty").each(function () {
        if ($(this).val() == "0" || $(this).val() === null) {
            $(this).val("X");
            $(this).css("color", "black");
        }
    });

    if ($("#tdddlOpenSizeFits").length > 0) {
        var className = $("select#fitDdl").val();
        if (className != '') {
            $("table#openSizeTbl tr,table#prePackTbl tr").each(function () {
                if ($(this).hasClass(className) == false)
                    if ($(this).hasClass("imP") == false) {
                        $(this).hide();
                    }
            });
        }
    }
});
$("select#fitDdl").change(function () {
    var className = $(this).val();
    var changed = false;
    if (className != '') {
        $("table#prePackTbl").show();
        $("table#openSizeTbl tr,table#prePackTbl tr").each(function () {

            if ($(this).hasClass(className) || $(this).hasClass("imP")) {
                $(this).show();
            }
            else {
                $(this).hide();
            }
        });
        if ($("table#prePackTbl tr." + className).length <= 0) {
            $("table#prePackTbl").hide();
        }
    }
    doCalc();
});
$(".cls1").focusin(function () {
    $(this).css("background", "white");
});
$(".cls1").focusout(function () {
    $(this).css("background", "none");
});
$(".numOnly").keydown(function (e) {
    // Allow: backspace, delete, tab, escape, enter and .
    if ($.inArray(e.keyCode, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
        // Allow: Ctrl+A
        (e.keyCode == 65 && e.ctrlKey === true) ||
        // Allow: home, end, left, right, down, up
        (e.keyCode >= 35 && e.keyCode <= 40)) {
        // let it happen, don't do anything
        return;
    }
    // Ensure that it is a number and stop the keypress
    if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
        e.preventDefault();
    }
});
$(".opcal").change(function () {
    var MaxVal = parseInt($(this).attr("data-val"));
    var currentVal = parseInt($(this).val());
    if (isNaN(MaxVal)) {
        MaxVal = 0;
    }
    if (!isNaN(currentVal)) {
        if (MaxVal < currentVal) {
            $(this).val("");
            alert("You cannot order more than " + MaxVal + " quantity for this pack");
        }
    }
    else {
        alert("You cannot order more than" + MaxVal + " quantity for this pack");
    }
    doCalc();
});
$("#btnOrderOpenSizes").click(function () {   
    var total = 0;
    $(".opcal").each(function () {
        if ($(this).is(":visible")) {
            $(this).siblings(".hidOSClass").val("0");
            var quant = $(this).val();
            if (!isNaN(quant)) {
                total += quant;
                $(this).siblings(".hidOSClass").val(quant);
            }
        }
        else {
            $(this).siblings(".hidOSClass").val(quant);
        }
    });
    if (total == 0) {
        alert("Please enter quantity for any size.");
    }
    else {

        addMeToo("cls1");
    }
});
$("#btnOrderPrePack").click(function () {   
    var total = 0;
    $(".cls1").each(function () {
        if ($(this).is(":visible")) {
            $(this).siblings(".hidAPClass").val("0");
            var quant = $(this).val();
            if (!isNaN(quant)) {
                total += quant;
                $(this).siblings(".hidAPClass").val(quant);
            }
        }
        else {
            $(this).siblings(".hidAPClass").val("0");
        }
    });
    if (total == 0) {
        alert("Please enter quantity for any size.");
    }
    else {

        addMeToo("opcal");
    }
});

$("#btnOrderAll").click(function () {   
    if ($("#userId").val() == "") {
        window.location.href = "/Home/Login?ReturnUrl=" + window.location.href;
    }
    else {
        var total = 0;
        $(".opcal").each(function () {
            if ($(this).is(":visible")) {
                $(this).siblings(".hidOSClass").val("0");
                var quant = $(this).val();
                if (!isNaN(quant)) {
                    total += quant;
                    $(this).siblings(".hidOSClass").val(quant);
                }
            }
            else {
                $(this).siblings(".hidOSClass").val(quant);
            }
        });
        $(".cls1").each(function () {
            if ($(this).is(":visible")) {
                $(this).siblings(".hidAPClass").val("0");
                var quant = $(this).val();
                if (!isNaN(quant)) {
                    total += quant;
                    $(this).siblings(".hidAPClass").val(quant);
                }
            }
            else {
                $(this).siblings(".hidAPClass").val("0");
            }
        });
        if (total == 0) {
            alert("Please enter quantity for any size.");
        }
        else {
            $("#ordForm").submit();
        }
    }
});
function addMeToo(className) {
    var total = 0;
    $("." + className).each(function () {
        if ($(this).is(":visible")) {
            var quant = $(this).val();
            if (className == "cls1")
                $(this).siblings(".hidAPClass").val("0");
            else if (className == "opcal")
                $(this).siblings(".hidOSClass").val("0");
            if (!isNaN(quant)) {
                total += quant;
                if (className == "cls1")
                    $(this).siblings(".hidAPClass").val(quant);
                else if (className == "opcal")
                    $(this).siblings(".hidOSClass").val(quant);
            }
        }
        else {
            if (className == "cls1")
                $(this).siblings(".hidAPClass").val("0");
            else if (className == "opcal")
                $(this).siblings(".hidOSClass").val("0");
        }
    });

    $("form").submit();
}

function checkMe(Total, elm) {

    var currentVal = parseInt($(elm).val());
    if (Total < currentVal) {
        $(elm).val("");
        alert("You cannot order more than " + Total + " quantity for this pack");
    }
    doCalc();
}

function doCalc() {
    var price=0;
    if($("#txtPrice").length>0)
        price = $("#txtPrice").val();
    else
        price = $("#txtMSRP").val();
    var total = 0;
    $(".opcal").each(function () {
        if ($(this).is(":visible")) {
            var quant = parseInt($(this).val());
            if (!isNaN(quant)) {
                total = total + quant;
            }
        }
    });
    $(".cls1").each(function () {
        if ($(this).is(":visible")) {
            var quant = parseInt($(this).val());
            var item = parseInt($(this).attr("title"));
            if (!isNaN(quant) && !isNaN(item)) {
                total = total + (quant * item);
            }
        }
    });
    $("#ttlQty").empty().text(total);
    $("#ttlPrc").empty().text("$ " + (parseFloat(price) * total).toFixed(2));
}

var p = $('#WindowDIV').offset();
$('#InsideArrowsDIV').show().css('left', p.left).css('top', p.top + 160);

if ($('.imgProduct').length < 2) {
    $('#btnNext').hide();
}
else {
    $('#btnNext').html('<img width="80" height="150" src="' + $($('.imgProduct')[1]).attr('src') + '" />');
}
p = $('#SliderDIV').offset();
var left = p.left + 500 - 200;
var top = p.top + $('#SliderDIV').height() - 192;

var nCurrentImage = 0;
function Move(i) {
    if ($("#SliderDIV").is(':animated') == false) {
        nCurrentImage += -(i);
        $('#MagnifyDIV').hide();
        var w = 495; // 257; // width of images + margin/gap between them

        var left = $('#SliderDIV').css('left').replace('px', '').replace('auto', '0');

        left = parseInt(left, 10) + (i * w);


        left = left >= 0 ? 0 : left; // Prevents from scrolling further back than there are cells
        var max = -($('#SliderDIV').width() - w);

        if (left >= 0) {
            $('#btnPrev').hide();
        }
        else {
            $('#btnPrev').show();
            $('#btnPrev').html('<img width="80" height="150" src="' + $($('.imgProduct')[nCurrentImage - 1]).attr('src') + '" />');
        }
        if (left < max) {
            $('#btnNext').hide();
        }
        else {
            $('#btnNext').show();
            $('#btnNext').html('<img width="80" height="150" src="' + $($('.imgProduct')[nCurrentImage + 1]).attr('src') + '" />');
        }
        if (($.browser.msie == true && $.browser.version >= 9) || ($.browser.mozilla == true)) {
            // Only IE 9 or higher can handle this animation nicely
            $('#SliderDIV').animate({
                left: left
            }, 1000);
        }
        else {
            $('#SliderDIV').css('left', left);
        }
    }
}