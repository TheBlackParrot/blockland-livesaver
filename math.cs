// ---------------------------- //
// Large Integer Math Functions //
// by Ipquarx (BL_ID 9291)		//
// ---------------------------- // ------------ //
// Allows for addition, subtraction,			//
// and multiplication of numbers of any size.	//
// --------------------------------------------	//

$log10 = mLog(10);
$log2 = mLog(2);
$l210 = $log10 * $log2;
$l217 = $log2 * mLog(17);

//Addition
function Math_Add(%num1, %num2)
{
	//Check if we can use torque addition
	if(strLen(%num1 @ %num2) < 7)
		return %num1 + %num2;
	
	//Account for sign rules
	if(%num1 >= 0 && %num2 >= 0)
		return stringAdd(%num1, %num2);
	if(%num1 < 0 && %num2 >= 0)
		return stringSub(%num2, switchS(%num1));
	if(%num1 < 0 && %num2 < 0)
		return "-" @ stringAdd(switchS(%num1), switchS(%num2));
	if(%num1 >= 0 && %num2 < 0)
		return stringSub(%num1, switchS(%num2));
	
	//dont know when this would happen but whatev
	return stringadd(%num1,%num2);
}

//Subtraction
function Math_Subtract(%num1, %num2)
{
	//Check if we can use torque subtraction
	if(strLen(%num1 @ %num2) < 7)
		return %num1 - %num2;
	
	//Account for sign rules
	if(%num1 >= 0 && %num2 >= 0)
		return stringSub(%num1, %num2);
	if(%num1 >= 0 && %num2 < 0)
		return stringAdd(%num1, switchS(%num2));
	if(%num1 < 0 && %num2 >= 0)
		return "-" @ stringAdd(%num2, switchS(%num1));
	if(%num1 < 0 && %num2 < 0)
		return stringSub(switchS(%num2), switchS(%num1));
	
	//don't know how this would happen, but ok
	return "Error";
}

//Multiplication
//0 for maxplaces means no limit, -1 means no decimal places
function Math_Multiply(%num1,%num2, %maxplaces)
{
	if(%maxplaces == 0)
		%maxplaces = 500;
	
	//Check if we can use torque multiplication
	if((%aaa=strLen(%num1 @ %num2)) < 6)
	{
		%ans = %num1 * %num2;
		%dont = true;
	}
	else
		%dont = false;
	if(!%dont)
	{
		//Here's a symbol you don't see everyday, the caret, AKA the XOR operator.
		if(%num1 < 0 ^ %num2 < 0)
			%ans = "-";
		if(%Num1 < 0)
			%Num1 = switchS(%Num1);
		if(%Num2 < 0)
			%Num2 = switchS(%Num2);
		
		%num1 = cleanNumber(%num1); %num2 = cleanNumber(%num2);
	}
	
	%a = getPlaces(%num1); %b = getPlaces(%num2);
	%c = getMax(%a, %b);
	if(!%dont)
	{
		if(%c != 0)
		{
			%places = %a + %b;
			%num1 = cleanNumber(strReplace(%num1, ".", ""));
			%num2 = cleanNumber(strReplace(%num2, ".", ""));
		}
		
		if(%aaa <= 80)
			%ans = %ans @ strMul(%num1, %num2);
		else
			%ans = %ans @ Karat(%num1, %num2);	
	}
	
	if(%c != 0)
	{
		if(!%dont)
			%ans = cleanNumber(placeDecimal(%ans, %places));
		if(%maxplaces > 0)
			%ans = cleanNumber(getIntPart(%ans) @ "." @ getSubStr(getDecPart(%ans), 0, %maxplaces));
		else if(%maxplaces != 0)
			%ans = getIntPart(%ans);
	}
		
	return %ans;
}

function Math_Pow(%num1, %num2)
{
	if(%num2 < 0)
		return 0;
	if(%num2 == 0 || %num1 == 1)
		return 1;
	
	return expon(%num1, %num2);
}

//This function switches the sign on a number.
function switchS(%a)
{
	if(%a < 0)
		return getSubStr(%a, 1, strLen(%a));
	return "-" @ %a;
}

//decimal shift right
function shiftRight(%a, %b)
{
	return (%c = strLen(%a)) > %b ? getSubStr(%a, 0, %c-%b) : "0"; 
}

//decimal shift left
function shiftLeft(%f,%a)
{
	if(%f$="0")return%f;
	%b = ~~(%a/32);
	
	%d="00000000000000000000000000000000";
	for(%b = %b; %b > 0; %b--)
		%c = %c @ %d;
	
	return (%e=%a%32) ? %f @ %c @ getSubStr(%d, 0, %e) : %f @ %c;
}

function strMul(%Num1,%Num2)
{
	if(%Num1 < 0)
		%Num1 = switchS(%Num1);
	if(%Num2 < 0)
		%Num2 = switchS(%Num2);
	
	%Len1 = strlen(%Num1);
	%Len2 = strlen(%Num2);
	
	for(%a = 0; %a < %len1; %a++)
		%x[%a] = getSubStr(%num1, %a, 1);
	
	for(%a = %Len2-1; %a >= 0; %a--)
	{
		%x = getSubStr(%num2,%a,1);
		for(%b = %Len1-1; %b >= 0; %b--)
			%Products[%a+%b] += %x*%x[%b];
	}
	
	%MaxColumn = %Len1 + %Len2 + strlen(%Products0) - 3;
	%MaxUse = %MaxColumn - %Len1 - %Len2 + 3;
	
	for(%a = %Len2 + %Len1 - 2; %a >= 0; %a--)
	{
		%b = strLen(%Products[%a]);
		%x = %MaxUse + %a;
		for(%b = %b - 1; %b >= 0; %b--)
			%Digits[%x--] += getSubStr(%Products[%a], %b, 1);
	}
	
	for(%a=%MaxColumn;%a>=0;%a--)
	{
		%Temp = %Digits[%a] + %Carry;
		if(%Temp > 9 && %a != 0)
		{
			%x = strLen(%temp) - 1;
			%Carry = getSubStr(%Temp, 0, %x);
			%Temp = getSubStr(%Temp, %x, %x + 1);
		}
		else
			%Carry = 0;
		
		%Result = %Temp @ %Result;
	}
	
	return %Result;
}

//Karatsuba multiplication algorithm
//This is faster than the classic multiplication for most numbers greater than 40 digits long.

function karat(%num1, %num2)
{
	%n = ~~((getmax(strLen(%num1), strLen(%num2)) + 1) / 2);
	
	if(%n < 150)
		return strMul(%num1,%num2);
	
	%x = shiftLeft("",%n);
	
	%b = shiftRight(%num1, %n);
	%a = stringsub(%num1, %b @ %x);
	%d = shiftRight(%num2, %n);
	%c = stringsub(%num2, %d @ %x);
	
	%ac = karat(%a, %c);
	%bd = karat(%b, $d);
	%abcd = karat(stringadd(%a, %b), stringadd(%c, %d));
	
	return stringadd(%ac, stringadd(%bd @ %x @ %x, stringsub(stringsub(%abcd, %ac), %bd) @ %x));
}

function stringAdd(%num1, %num2)
{
	%a = getDecimal(%num1); %b = getDecimal(%num2);
	%decPlace = getmax(%a, %b);
	if(%decPlace != -1)
	{
		if(%a == -1 && %b != -1)
			%num1 = %num1 @ ".0";
		else if(%b == -1 && %a != -1)
			%num2 = %num2 @ ".0";
		%x = equ0sd(%num1, %num2);
		%a = getDecimal(%num1); %b = getDecimal(%num2);
		%decPlace = getmax(%a, %b);
		%num1 = strreplace(getWord(%x, 0), ".", "");
		%num2 = strreplace(getWord(%x, 1), ".", "");
	}
	else
	{
		%x = equ0s(%num1, %num2);
		%num1 = getWord(%x, 0);
		%num2 = getWord(%x, 1);
	}
	for(%a=0;%a<strLen(%num1);%a++)
	{
		%start[%a] = getSubStr(%num1, %a, 1);
		%adder[%a] = getSubStr(%num2, %a, 1);
	}
	%Length = strLen(%num1);
	for(%a = %Length - 1; %a >= 0; %a--)
	{
		%res = %start[%a] + %adder[%a] + %Carry;
		if(%res > 9 && %a != 0)
		{
			%Carry = 1;
			%Ans[%a] = %res - 10;
			continue;
		}
		if(%res < 10)
			%Carry = 0;
		%Ans[%a] = %res;
	}
	for(%a = 0; %a < %length; %a++)
	{
		if(%a == %decPlace)
		{
			%Answer = %Answer @ "." @ %Ans[%a];
			continue;
		}
		%Answer = %Answer @ %Ans[%a];
	}
	if(%decplace > 1)
		%Answer = stripend0s(%Answer);
	return %Answer;
}

function stringSub(%num1, %num2)
{
	%a = getDecimal(%num1); %b = getDecimal(%num2);
	%decPlace = getmax(%a, %b);
	if(%decPlace != -1)
	{
		if(%a == -1 && %b != -1)
			%num1 = %num1 @ ".0";
		else if(%b == -1 && %a != -1)
			%num2 = %num2 @ ".0";
		%x = equ0sd(%num1, %num2);
		%a = getDecimal(%num1); %b = getDecimal(%num2);
		%decPlace = getmax(%a, %b);
		%num1 = strreplace(getWord(%x, 0), ".", "");
		%num2 = strreplace(getWord(%x, 1), ".", "");
	}
	else
	{
		%x = equ0s(%num1, %num2);
		%num1 = getWord(%x, 0);
		%num2 = getWord(%x, 1);
	}
	for(%a=0;%a<strLen(%num1);%a++)
	{
		%start[%a]=getSubStr(%num1,%a,1);
		%subtractor[%a]=getSubStr(%num2,%a,1);
	}
	if(%num1 < %num2)
		return "-" @ stringSub(%num2, %num1, %x);
	if(%num1 $= %num2)
		return "0";
	%Length = strLen(%num1);
	for(%a = %Length - 1; %a >= 0; %a--)
	{
		%res = %start[%a] - %subtractor[%a];
		if(%res < 0)
		{
			for(%b=%a-1;%b>=0;%b--)
			{
				if(%start[%b] - %subtractor[%b] > 0)
				{
					%start[%b] -= 1;
					for(%c=%b + 1;%c<%a;%c++)
						%start[%c] += 9;
					%start[%a] += 10;
					break;
				}
			}
			%res = %start[%a] - %subtractor[%a];
		}
		%Ans[%a] = %res;
	}
	%trim = true;
	for(%a = 0; %a < %length; %a++)
	{
		if(%Ans[%a] == 0 && %trim == true && %a != %decPlace - 1)
			continue;
		if(%a == %decPlace)
		{
			%Answer = %Answer @ "." @ %Ans[%a];
			continue;
		}
		%Answer = %Answer @ %Ans[%a];
		%trim = false;
	}
	if(%decplace > 1)
		%Answer = stripend0s(%Answer);
	return %Answer;
}

//This function equalises the length of two numbers by adding zeroes behind the shorter one.
function equ0s(%num1, %num2, %mod)
{
	%x = strLen(%num1); %y = strLen(%num2);
	if(!%mod)
	{
		if(%x < %y)
			%num1 = shiftLeft("", %y - %x) @ %num1;
		else if(%x > %y)
			%num2 = shiftLeft("", %x - %y) @ %num2;
	}
	else
	{
		if(%x < %y)
			%num1 = %num1 @ shiftLeft("", %y - %x);
		else if(%x > %y)
			%num2 = %num2 @ shiftLeft("", %x - %y);
	}
	return %num1 SPC %num2;
}


function expon(%a, %b, %d)
{
	if(%b == 0)
		return 1;
	else if(%b < 0)
		return expon(1/%a, -1 * %b, %d++);
	else if(%b % 2 == 1)
	{
		%c = expon(%a, (%b - 1) / 2, %d++);
		return Math_Multiply(%a, Math_Multiply(%c, %c));
	}
	else if(%b % 2 == 0)
	{
		%c = expon(%a, %b / 2, %d++);
		return Math_Multiply(%c, %c);
	}
}

function stripend0s(%i)
{
	if(%i $= "")
		return"";
	for(%a=0;%a<strLen(%i);%a++)
		%i[%a] = getSubStr(%i, %a, 1);
	%trim = true;
	for(%a=strLen(%i)-1;%a>-1;%a--)
	{
		if(%trim == true && %i[%a] $= "0")
		{
			%i[%a] = "";
			continue;
		}
		else if(%trim == true && %i[%a] $= ".")
		{
			%i[%a] = "";
			continue;
		}
		%trim = false;
	}
	for(%a=0;%a<strLen(%i);%a++)
		%b = %b @ %i[%a];
	return %b;
}

function getIntPart(%i)
{
	if(strpos(%i, ".") == -1)
		return %i;
	return getSubStr(%i, 0, strLen(%i) - strLen(strchr(%i, ".")));
}

function getDecPart(%i)
{
	if(strPos(%i, ".") == -1)
		return"";
	return getSubStr(strChr(%i, "."), 1, 99999);
}

function getDecimal(%i)
{
	return strpos(%i, ".");
}

function equ0sd(%num1, %num2)
{
	%a = getIntPart(%num1); %b = getIntPart(%num2);
	%c = (%c=getDecPart(%num1)) $= "" ? 0 : %c; %d = (%d=getDecPart(%num2)) $= "" ? 0 : %d;
	
	%e = equ0s(%a, %b);
	%f = equ0s(%c, %d, 1);
	return getWord(%e, 0) @ "." @ getWord(%f, 0) @ " " @ getWord(%e, 1) @ "." @ getWord(%f, 1);
}

//Equivalent to multiplying by 10^-%place
function placeDecimal(%num, %place)
{
	if(strPos(%num, ".") != -1 || %place == 0)
		return %num;
	%log = strLen(%num);
	%pos = %log - %place;
	if(%pos <= 0)
	{
		%start = 0;
		%end = shiftLeft("", -%pos) @ %num;
	}
	else
	{
		%start = getSubStr(%num, 0, %pos);
		%end = getSubStr(%num, %pos, 9999);
	}
	
	return %start @ "." @ %end;
}

function getPlaces(%num)
{
	if(strPos(%num, ".") == -1)
		return 0;
	%num = stripend0s(%num);
	return getMax(strLen(strChr(%num, ".")) - 1, 0);
}

function cleanNumber(%num)
{
	%a = strReplace(lTrim(strReplace(getIntPart(%num), "0", " ")), " ", "0");
	//echo(%a @ " AAA");
	%b = stripend0s(getDecPart(%num));
	//echo(%b);
	if(%a $= "" && %b !$= "")
		%a = 0;
	return %a @ (%b !$= "" ? "." @ %b : "");
}

//integers only for now
function Math_Divide(%n, %d, %q)
{
	%qo = %q;
	if(%qo == 0)
		%qo=-1;
	%aa = strLen(getIntPart(%n));
	%bb = strLen(getIntPart(%d));
	%xx = mAbs(%aa - %bb) + 1;
	%q *= 2;
	%q = getMax(%xx,%q);
	
	%e = Math_Multiply(getMax(strLen(getIntPart(%d))-1,1), 3.321928, -1);
	%dd = Math_Multiply(%d, Math_Pow(0.5, %e), %q);
	while(%dd > 1)
	{
		%dd = Math_Multiply(0.5, %dd, %q);
		%e++;
	}
	%n = Math_Multiply(%n, Math_Pow(0.5, %e), %q);
	%x = Math_Subtract(2.823, Math_Multiply(1.882, %dd, %q));
	%x=%dd;
	while(true)
	{
		%x = Math_Multiply(%x, Math_Subtract(2, Math_Multiply(%dd, %x, %q)), %q);
		%z++;
		if(%lastx $= %x)
		{
			if(%z < %xx)
				continue;
			break;
		}
		%lastx = %x;
	}
	return Math_Multiply(%n,%x,%qo);
}

function Math_DivideFloor(%n, %d)
{
	%aa = strLen(getIntPart(%n));
	%bb = strLen(getIntPart(%d));
	%xx = mAbs(%aa - %bb) + 2;
	
	%e = Math_Multiply(getMax(strLen(getIntPart(%d))-1,1), 3.321928, -1);
	%z = Math_Pow(0.5, %e);
	%zz = %e;
	%dd = Math_Multiply(%d, %z, %xx);
	while(%dd > 1)
	{
		%dd = Math_Multiply(0.5, %dd, %xx);
		%e++;
	}
	
	%n = Math_Multiply(%n, Math_Multiply(%z, Math_Pow(0.5, %e-%zz)), %xx);
	%x = Math_Subtract(2.823, Math_Multiply(1.882, %dd, %xx));
	for(%z = 0; %z < %xx; %z++)
	{
		%x = Math_Multiply(%x, Math_Subtract(2, Math_Multiply(%dd, %x, %xx)), %xx);
		if(%lastx $= %x || %z >= %xx)
			break;
		%lastx = %x;
	}
	return Math_Multiply(%n,%x,-1);
}

function Math_Mod(%a,%b)
{
	%x = %b;
	%y = strLen(%a); %z = strLen(%b);
	
	if(alessthanb(%a,%b))
		return %a;
	else if((%aa=~~(3.4*(%y-%z))) > 0)
		%b = Math_Multiply(%b, Math_Pow(2, %aa));
	
	while(!alessthanb(%a, %b))
		%b = Math_Multiply(%b, 2);
	
	while(!alessthanb(%a, %x))
	{
		if(alessthanb(%a,%b))
		{
			%aa = ~~(3.4 * (strLen(%b) - strLen(%a)));
			%b = Math_Multiply(%b, Math_Pow(0.5, getMax(1,%aa)), -1);
			
			while(alessthanb(%a, %b))
				%b = Math_Multiply(%b, 0.5, -1);
		}
		
		%a = Math_Subtract(%a, %b);
	}
	return %a;
}

function divider(%numer, %denom, %numDec)
{
	%result = Math_DivideFloor(%numer, %denom);
	%rem = Math_Subtract(%numer, Math_Multiply(%denom, %result)) @ "0";
	echo(%result SPC %rem);
	%result = %result @ ".";
	for (%i=0;%i<%numDec;%i++)
	{
		%x = Math_DivideFloor(%denom, %rem);
		echo(%x);
		%result = %result @ %x;
		%rem = Math_Subtract(%rem, Math_Multiply(%denom, %x)) @ "0";
	}
	return %result;
}