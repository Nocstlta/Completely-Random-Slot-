using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Completely_random_slot;

// C# Shell には存在するのに本物の .NET MAUI には無い DisplayAlertAsync を補う互換レイヤー。
// MainPage.cs には一切手を入れず、このファイルが両環境の差異を吸収する。
public static class DisplayAlertCompat
{
	public static Task DisplayAlertAsync(this Page page, string title, string message, string cancel)
		=> page.DisplayAlert(title, message, cancel);

	public static Task<bool> DisplayAlertAsync(this Page page, string title, string message, string accept, string cancel)
		=> page.DisplayAlert(title, message, accept, cancel);
}
