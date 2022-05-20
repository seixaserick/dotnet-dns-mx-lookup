
/*

Author: <seixaserick77@gmail.com> 

 */


while (true)
{

    // Asks email to be verified
    Console.WriteLine("Type the email address you want to verify domain MX records (example: user@gmail.com)?");

    // Read user input
    string? inputText = Console.ReadLine().Trim();


    // Verify user input  
    if (string.IsNullOrEmpty(inputText) || inputText.Split("@").Length != 2)
    {
        Console.Clear();
        Console.WriteLine(@$"Your input was ""{inputText}"" and this isn't a valid email.");
        continue; // Ask again
    }


    // Detect email's domain  
    string emailDomain = inputText.Split("@")[1].Trim();

    // Verify public domain 
    if (string.IsNullOrEmpty(emailDomain) || emailDomain.Split(".").Length < 2 || string.IsNullOrEmpty(emailDomain.Split(".")[1])
       )
    {
        Console.Clear();
        Console.WriteLine(@$"Your input was ""{inputText}"" and this isn't a valid email because this program just check public domains.");
        continue; // Ask again
    }

    Console.WriteLine(@$"The ""{inputText}"" has the domain ""{emailDomain}"" and now we will lookup DNS MX records...");


    // Comma-separated DNS server list (IPs). Google public DNS servers ("8.8.8.8" or "8.8.4.4.") are used in this example for quering MX records. You can change this DNS servers to your prefered DNS servers.
    var dnsServersList = new List<string>("8.8.8.8,8.8.4.4".Split(',')); 

    var dnsLite = new DnsLib.DnsLite();
    dnsLite.setDnsServers(dnsServersList);
    var dnsMxRecords = dnsLite.GetMXRecords(emailDomain);

    if (dnsMxRecords.Count == 0)
    {
        Console.WriteLine(@$"The domain ""{emailDomain}"" has no MX DNS records!");
    }

    // Print all mail servers found and its preferences (priorities)
    foreach (var dnsMxRecord in dnsMxRecords)
    {
        Console.WriteLine($"DNS server [{dnsMxRecord.dnsServer}] answer: {dnsMxRecord}");
    }

    // Asks the user if want to run again
    if (!RunAgain())
    {
        Console.WriteLine("Bye bye. Program finished!");
        break; //exist from loop, closing the program.
    }


}



bool RunAgain()
{
    // This function is called just to ask if the user wants to run again the program
    Console.WriteLine("Run again (Y/N)?");
    var inputText = Console.ReadLine();
    return inputText.ToUpper() == "Y"; //return true if user's input is Y or y
}


