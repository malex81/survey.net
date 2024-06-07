using Grpc.Core;
using Grpc.Net.Client;
using GrpsReview.Client;

// создаем канал для обмена сообщениями с сервером
// параметр - адрес сервера gRPC
using var channel = GrpcChannel.ForAddress("http://localhost:5135");
await TestStreem(channel);
//await TestGeeter(channel);

static async Task TestGeeter(GrpcChannel channel)
{
	// создаем клиент
	var client = new Greeter.GreeterClient(channel);
	Console.Write("Введите имя: ");
	string? name = Console.ReadLine();
	// обмениваемся сообщениями с сервером
	var reply = await client.SayHelloAsync(new HelloRequest { Name = name });
	Console.WriteLine($"Ответ сервера: {reply.Message}");
	Console.ReadKey();
}

static async Task TestStreem(GrpcChannel channel)
{
	var client = new Messenger.MessengerClient(channel);

	// посылаем пустое сообщение и получаем набор сообщений
	var serverData = client.ServerDataStream(new Request());

	// получаем поток сервера
	var responseStream = serverData.ResponseStream;

	while (await responseStream.MoveNext(new CancellationToken()))
	{
		Response response = responseStream.Current;
		Console.WriteLine($"{DateTime.Now:ss.ffff}: {response.Content}");
	}

	//await foreach (var response in responseStream.ReadAllAsync())
	//{
	//	Console.WriteLine(response.Content);
	//}
}