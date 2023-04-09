$(document).ready(function(){
	var screenHeight = $(window).height();
	var screenWidth = $(window).width();
	//$('.container').css('height', screenHeight+60);
	/*$('.homeContGlowBg').css('height', screenHeight+60);
	$('.blackShadow').css('height', screenHeight+60);
	$('.homeLeft').css('height', screenHeight+60);
	$('.homeRight').css('height', screenHeight+60);
	$('.homeLeftLogo').css({top:(screenHeight-110)/2});
	$('.homeRedRibban').css({top:(screenHeight-233)/2});
	$('.homeYellowRibban').css({top:(screenHeight-305)/2});
	$('.seeHow').css({top:(screenHeight-90)});
	$('.homeYellowRibbanCont').css({left:screenWidth-400});*/
	
	$(window).resize(function(){
		var screenHeight = $(window).height();
		//$('.container').css('height', screenHeight+60);
		/*$('.homeContGlowBg').css('height', screenHeight+60);
		$('.blackShadow').css('height', screenHeight+60);
		$('.homeLeft').css('height', screenHeight+60);
		$('.homeRight').css('height', screenHeight+60);
		$('.homeLeftLogo').css({top:(screenHeight-110)/2});
		$('.homeRedRibban').css({top:(screenHeight-233)/2});
		$('.homeYellowRibban').css({top:(screenHeight-305)/2});
		$('.seeHow').css({top:(screenHeight-90)});*/
	});
	
	$('.homeLearnMore').click(function(){
		$('.homeRedRibbanCont').animate({left:screenWidth-400}, function(){
			$('.homeRedRibbanCont').css('display', 'none');
			$('.homeYellowRibbanCont').css('display', 'block');
			$('.homeYellowRibbanCont').animate({left:0});
		});
		
	});
	
	$('.seeHow').hover(function(){
		$(this).stop().animate({marginTop:10});
	},function(){
		$(this).stop().animate({marginTop:0});
	});
	
	$('.seeHow').click(function(){
		var homeContHeight = $('.homeCont').outerHeight();
		$('html, body').animate({
			 scrollTop: homeContHeight
		 },
		 1000);
		$('.menuCont ul li').removeClass('active');
		$('.menuCont ul li a:contains("How it works")').parents('li').addClass('active');
		 return false;
	});
	
	$('.menuCont ul li a').click(function(){
		var homeContHeight = $('.text1').outerHeight();
		var howItWorkHeight = $('.text2').outerHeight();
		var working = $('.text3').outerHeight();
		var help = $('.text4').outerHeight();
		
		var thisText = $(this).text();
		//alert(thisText);
		if(thisText == 'Search & Apply'){
			$('html, body').animate({
				 scrollTop: homeContHeight
			 },
			 1000);
			$('.menuCont ul li').removeClass('active');
			$(this).parents('li').addClass('active');
		}else if(thisText == 'Overview'){
			$('html, body').animate({
				 scrollTop: 0
			 },
			 1000);
			$('.menuCont ul li').removeClass('active');
			$(this).parents('li').addClass('active');
		}else if(thisText == 'Working at Platini'){
			$('html, body').animate({
				 scrollTop: howItWorkHeight+homeContHeight
			 },
			 1000);
			$('.menuCont ul li').removeClass('active');
			$(this).parents('li').addClass('active');
		}else if(thisText == 'Help'){
			$('html, body').animate({
				 scrollTop: working+howItWorkHeight+homeContHeight
			 },
			 1000);
			$('.menuCont ul li').removeClass('active');
			$(this).parents('li').addClass('active');
		}
	});
	
	$(window).scroll(function(){
		var scrollTopPos = $(document).scrollTop();
		var homeContHeight = $('.homeCont').outerHeight();
		var howItWorkHeight = $('.howItWorkCont').outerHeight();
		var faqHeight = $('.faqCont').outerHeight();

		if(scrollTopPos == 0){
			$('.menuCont ul li').removeClass('active');
			$('.menuCont ul li a:contains("Home")').parents('li').addClass('active');
		}else if(scrollTopPos <= (homeContHeight+(howItWorkHeight/2))){
			$('.menuCont ul li').removeClass('active');
			$('.menuCont ul li a:contains("How it works")').parents('li').addClass('active');
		}else if(scrollTopPos <= (homeContHeight+howItWorkHeight)){
			$('.menuCont ul li').removeClass('active');
			$('.menuCont ul li a:contains("FAQ")').parents('li').addClass('active');
		}
	});
	
	$('.faqQuestions ul li span').click(function(){
		$('.faqQuestions ul li .answer').slideUp('slow');
		if($(this).parents('li').find('.answer').is(':hidden')){
			$(this).parents('li').find('.answer').slideDown('slow');
		}else{
			$(this).parents('li').find('.answer').slideUp('slow');
		}
	});
	
	$('.trialAlertClose').click(function(){
		$(this).parents('.trialAlert').fadeOut('slow');
	});
	
	$('.dashboardNormalCont').each(function(){
		var getThisHeight = $(this).outerHeight();
		var getLearnHeight = $(this).find('.learnMore').outerHeight();
		$(this).find('.learnMore').css({top:(getThisHeight-getLearnHeight)/2})
	});
	
});