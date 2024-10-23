using HtmlAgilityPack;

namespace ParserSurvey;
static class Test1
{
	// https://html-agility-pack.net/from-string
	public static void RunDocumantationExample()
	{
		var html =
		@"<!DOCTYPE html>
<html>
<body>
	<h1>This is <b>bold</b> heading</h1>
	<p>This is <u>underlined</u> paragraph</p>
	<h2>This is <i>italic</i> heading</h2>
</body>
</html>";

		var htmlDoc = new HtmlDocument();
		htmlDoc.LoadHtml(html);

		var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

		Console.WriteLine(htmlBody.OuterHtml);
	}

	public static void RunSimpleTest()
	{
		var html = @"Привет, Вася!
<br>Как <small>твоя жалкая жизнь</small>?
Не хочешь <custom-widget fuck-off='ушел в себя' fuck-in='Вернусь не скоро'/> повеситься?
<p>А тут будет параграф
<p>И еще один";
		var htmlDoc = new HtmlDocument();
		htmlDoc.LoadHtml(html);

		Console.WriteLine(htmlDoc.DocumentNode.OuterHtml);
	}
}
