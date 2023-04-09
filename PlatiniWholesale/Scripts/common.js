


$().ready(function () {
    $('.closeSearchAgent').click(function () {
        $(this).parents('#searchfieldset').fadeOut('slow');
    });

    $('div.admin_main:eq(1)').css("min-height", $(window).height() - $('div.admin_main:eq(0)').height() - $('#footer').height() - 12);

    $("#tbllist tr:last td").removeClass('boxborder1').addClass('borderright');

    $(':submit').click(function () {
        $('span.error').remove();
        $('label.error').remove();
    });
    TabSelection();


});



$().ready(function () {
    $("#searchtab").click(function () {
        $('#searchfieldset').show();

    });
    
});


function languageFormSubmit() {
    $('#forlanaguage').submit();
}

function sortByFormSubmit() {
    $('#sortByForm').submit();
}

function getState(countryID) {
    $("#ajax-loader").html('<img src="' + ROOTIMAGEDIR + 'ajax-loader.gif">');
    $("#ajax-loader").show();
    $("#stateID").hide();
    $.ajax({
        url: 'ajaxFunction.php?countryID=' + countryID,
        type: 'post',
        data: countryID,
        cache: false,
        dataType: 'json',
        success: function (data) {
            $('#stateID').empty();
            $("#ajax-loader").hide();
            $("#stateID").show();
            var i;
            for (i = 0; i <= count(data) ; i++) {
                $('#stateID').append('<option value="' + data[i]['stateID'] + '">' + data[i]['stateName'] + '</option>');
            }
        }
    });
}

// Ban Any Single Record
function adminActionSingleRecord(action, actionField, actionFieldValue, actionOn, actionOnValue, actionCallBack, actionUrl, actionMessage) {
    if (action != '' && actionField != '' && actionFieldValue != '' && actionOn != '' && actionOnValue != '' && actionUrl != '') {
        switch (action) {
            case 'ban': message = "Do you want to change display status?"; break;
            case 'delete': message = "Are you sure to delete this information?"; break;
            case 'confirm': message = "Are you sure to confirm this member?"; break;
            default: message = "Do you want to perform this action?"; break;
        }
        var url = new Array();
        url = document.location.href.split('?');
        url = url[0];
        if (typeof (actionUrl) === 'undefined') actionUrl = base64_encode(url);
        if (typeof (actionMessage) === 'undefined') actionMessage = message;
        if (typeof (actionCallBack) === 'undefined') actionCallBack = base64_encode(document.location.href);
        var conf = confirm(base64_decode(actionMessage));
        if (conf) {
            actionUrl = base64_decode(actionUrl) + '?' + actionField + '=' + actionFieldValue + '&' + actionOn + '=' + actionOnValue;
            document.location.href = actionUrl + "&callbackurl=" + actionCallBack;
        }
    }
}


$('.openMemberDetails').click(function () {
    var id = $(this).attr('rel');
    uri = 'view_member_detail.php?memberID=' + id;
    OpenPageFancyBox(uri, 675, 575);
    //var url = 'view_member_detail.php?memberID='+id;
    //window.open(url,'popup_player','height=550,width=600,top=150,left=300,scrollbars=yes');	
});

/*get the list for referral code of all the RS members*/
$('.getReferral').click(function () {
    uri = "referral_code_list.php";
    OpenPageFancyBox(uri, 675, 575);
});




function closeMessage() {
    window.close();
    return false;
}
function copyReferral(obj) {
    var referralID = $(obj).attr('rel');
    parent.$("#referralID").val(referralID);
    parent.$.fancybox.close();
}



function resetEditor(editorParam) {
    var x = editorParam.split(',');
    for (i = 0; i < x.length; i++) {
        var MyFCKeditor___Frame = document.getElementById(x[i] + "___Frame");
        MyFCKeditor___Frame.src = ADMINJSURL + "fckeditor/editor/fckeditor.html?InstanceName=" + x[i] + "&Toolbar=Default";
        return false;
    }
}


function CheckAll() {
    for (var i = 0; i < document.myForm.elements.length; i++) {
        if (document.myForm.elements[i].type == 'checkbox') {
            if (document.myForm.mainCheckbox.checked == true) {
                document.myForm.elements[i].checked = true;
            }
            if (document.myForm.mainCheckbox.checked == false) {
                document.myForm.elements[i].checked = false;
            }
        }
    }
}

// this javascript function work on click of delete Selected button to delete records 
function onDelete() {
    var count = 0;
    for (var i = 0; i < document.myForm.elements.length; i++) {
        if (document.myForm.elements[i].type == 'checkbox') {
            if (document.myForm.elements[i].checked == true) {
                var count = count + i;
            }
        }
    }

    if (count == 0) {
        alert("No record(s) selected. Please select at least one record to delete.");
        return false;
    }
    else {

        if (confirm("Are you sure to delete information?")) {
            $('#myForm').submit();
        }
        else {
            return false;
        }
    }
}


/*Dynamic Email tamplet variable insertion*/
function InsertHTML(data) {
    // Get the editor instance that we want to interact with.
    var oEditor = FCKeditorAPI.GetInstance('message');

    // Check the active editing mode.
    if (oEditor.EditMode == FCK_EDITMODE_WYSIWYG) {
        // Insert the desired HTML.
        oEditor.InsertHtml(data);
    }
}


//This function is for slider in Magazine Page
$().ready(function () {
    $('#hideSideBar').click(function () {
        $('.leftSlideList').animate({ width: 25 });
        $('.leftSlideList ul').animate({ width: 0, height: 0 });
        $('.slideData').animate({ marginLeft: 32 });
        $('#showSideBar').fadeIn('slow');
        $(this).fadeOut('fast');
        $(this).parents('#showHideDiv').find('.slideTitle').fadeOut('fast');
        //$('.leftSlideList').css('borderRight', '0px');
    });

    $('#showSideBar').click(function () {
        $(this).fadeOut('fast');
        $('#hideSideBar').fadeIn('slow');
        $('.slideData').animate({ marginLeft: 275 });
        $('.leftSlideList').animate({ width: 248 });
        $('.leftSlideList ul').animate({ width: 250, height: 593 });
        $(this).parents('#showHideDiv').find('.slideTitle').fadeIn('fast');
        //$('.leftSlideList').css('borderRight', '1px solid #59A5CB');
    });

    //This code is used for tabs in property add/edit
    $('.tabs').click(function () {
        var divID = $(this).attr('id').split('_');
        var isValid = true;
        $('#tabdivid_1').find("input, select, textarea").each(function () {
            if ($(this).hasClass("required") && isValid) {
                isValid = $(this).valid();
            }

        });
        if (isValid) {
            $('.tabs').removeClass('tabs ui-tabs-selected');
            $(this).addClass('tabs ui-tabs-selected');

            $('.tabdiv').hide();
            $('#tabdivid_' + divID[1]).show();
        } else {

        }
    });
});



// to change the pagelimit 



//For left section of admin panel
var isDone = false;
$(document).ready(function () {
    //if(	$('.mainMenu2').find('ul li ul li a').hasClass('selected'))
    $('body').find('.selected').parents('.submenu').show();

    var screenHeight = $(window).height();
    $('.leftSec').css('minHeight', (screenHeight));
    $('.rightSec').css('minHeight', (screenHeight));
    $('.drag-drop-box').css('minHeight', (screenHeight - 250));


    $(".mainMenu2 ul li").click(function () {
        if (isDone == false) {
            $(".mainMenu2 ul li").find(".submenu").hide();
            $(this).find(".submenu").slideToggle("slow");
            //$(this).next().slideToggle('slow');
            }
    });

    $(".mode_switcher").click(function () {
        TabSelection();
        $(".submenu").hide();
    });

    $('.mode_switcher').click(function () {
        $('.leftSec').toggleClass('navIcon');
        if ($('.leftSec').hasClass('navIcon')) {
            $('.text').hide();
            $('.leftSec').animate({ width: 61 });
            $('.rightSec').animate({ marginLeft: 61 });
            $('.header_left img').animate({ width: 40, height: 42 });
            isDone = true;
        } else {
            $('.leftSec').animate({ width: 221 });
            $('.rightSec').animate({ marginLeft: 221 });
            $('.text').show();
            $('.text').css('display', 'inline');
            $('.header_left img').animate({ width: 50, height: 52 });
            isDone = false;
        }
    });

    $('.navIcon .middle-left .mainMenu2 ul li').on('mouseover', function () {
        $(this).find('.text').css('display', 'inline');
    });

    $('.navIcon .middle-left .mainMenu2 ul li').on('mouseout', function () {
        $(this).find('.text').css('display', 'none');
    });

    $(".collapse").click(function () {
        $(this).parent('.head').next(".outer-box").slideToggle("slow");
    });

    $(".dekstop-box-btn").click(function () {
        $(".dox-contaner").slideToggle("slow");
    });


    $(".deactive").click(function () {
        if ($(this).parents('tr').next(".detai-pannel").is(':hidden')) {
            $(this).parents('tr').next(".detai-pannel").slideDown('slow');
            $(this).css('backgroundPosition', '-20px 4px');
        } else {
            $(this).parents('tr').next(".detai-pannel").slideUp('slow');
            $(this).css('backgroundPosition', '10px 4px');
        }
    });


    $(".filtertab").click(function () {
        $("#filterblock").slideToggle("slow");
    });



    $(".rmve").click(function () {
        $("#filterblock").slideToggle("slow");
    });

    // $(".addlisting").click(function(){
    //$("#addlisting").slideToggle("slow");
    //});




    var screenHeight = $(window).height();
    $('.mainbodycontent').css('minHeight', (screenHeight - 170));



    var screenHeight = $(window).height();
    $('.minContainer').css('minHeight', (screenHeight - 266));

});

//function to go back to previous url
function prevUrl() {
    window.history.back();
}





function confirmDeleteUser() {
    var agree = confirm("Are you sure you want to delete this user?");
    if (agree)
        return true;
    else
        return false;
}
function confirmDeleteTicket() {
    var agree = confirm("Are you sure you want to delete this ticket?");
    if (agree)
        return true;
    else
        return false;
}
function confirmDeletetemplate() {
    var agree = confirm("Are you sure you want to delete this email template?");
    if (agree)
        return true;
    else
        return false;
}
function confirmDeletePage() {
    var agree = confirm("Are you sure you want to delete this page?");
    if (agree)
        return true;
    else
        return false;
}
function TabSelection() {
    var url = window.location.pathname;
    if (url.indexOf('Location') != -1) {
        var lnk = $('#managelocation').next('.submenu');
        $('#managelocation').addClass('active');
        lnk.show();
        if (url.indexOf('Countries') != -1)
            lnk.find('a:eq(0)').addClass('selected');
        if (url.indexOf('State') != -1)
            lnk.find('a:eq(1)').addClass('selected');
        if (url.indexOf('Cities') != -1)
            lnk.find('a:eq(2)').addClass('selected');
    }
    if (url.indexOf('Option') != -1) {
        var lnk = $('#manageoption').next('.submenu');
        $('#manageoption').addClass('active');
        lnk.show();
        if (url.indexOf('Value') != -1)
            lnk.find('a:eq(1)').addClass('selected');
        if (url.indexOf('Group') != -1)
            lnk.find('a:eq(0)').addClass('selected');
    }
    if (url.indexOf('User') != -1) {
        var lnk = $('#manageuser').next('.submenu');
        $('#manageuser').addClass('active');
        lnk.show();
        if (url.indexOf('AllUser') != -1)
            lnk.find('a:eq(0)').addClass('selected');
        else
            lnk.find('a:eq(1)').addClass('selected');
    }
}

function ShowHelp(l, q) {
    l.id || (l = document.getElementById(q + "_source")); var D = document.getElementById(q + "_help"); if (D) if (D.style.display == "") { D.style.display = "none"; var I = D.getElementsByTagName("DIV")[0]; I.style.padding = "0px" } else {
        D.style.display = ""; I = D.getElementsByTagName("DIV")[0]; I.style.padding = "10px"; I = 340; if (D.style.width) I = parseInt(D.style.width.replace("px", "")); var Q = helpGetOffsetLeft(l), W = window.innerWidth ? window.innerWidth - 25 : document.body.clientWidth; if (Q + I > W) Q = W - I; newX = Q; I = D.offsetHeight;
        Q = helpGetOffsetTop(l) + l.offsetHeight; W = window.innerHeight ? window.innerHeight - 25 : document.body.clientHeight; if (Q + I > (W < 500 ? 500 : W) + helpGetScrollY()) Q = helpGetOffsetTop(l) - I; newY = Q; D.style.top = newY + "px"; D.style.left = newX + "px"
    }
} function helpGetOffsetTop(l) { var q = l.offsetTop; for (l = l.offsetParent; l;) { q += l.offsetTop; l = l.offsetParent } return q } function helpGetOffsetLeft(l) { var q = l.offsetLeft; for (l = l.offsetParent; l;) { q += l.offsetLeft; l = l.offsetParent } return q }