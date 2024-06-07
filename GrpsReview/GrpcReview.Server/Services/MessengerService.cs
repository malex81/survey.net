using Grpc.Core;

namespace GrpsReview.Services;
// source: https://metanit.com/sharp/grpc/2.5.php
public class MessengerService : Messenger.MessengerBase
{
	private readonly string[] messages = { "Привет", "Как дела?", "Че молчишь?", "Ты че, спишь?", "Ну пока" };
	public override async Task ServerDataStream(Request request,
		IServerStreamWriter<Response> responseStream,
		ServerCallContext context)
	{
		int num = 1;
		foreach (var message in messages)
		{
			await responseStream.WriteAsync(new Response { Content = (num++).ToString() });
			await responseStream.WriteAsync(new Response { Content = message });
			// для имитации работы делаем задержку в 1 секунду
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
	}
}