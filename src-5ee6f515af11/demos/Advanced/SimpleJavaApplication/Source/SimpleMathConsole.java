import java.io.*;
import java.util.StringTokenizer;
import java.util.Arrays;

public class SimpleMathConsole {

		String USAGE = "SimpleMathConsoleApp.exe -sum -mul <integer> -infile <filename> -outfile <filename> -wait <timeinms>";
		
		public static void main(String[] args) {
                try
                {
						for (int i = 0; i < args.length; i++)
						{
							 System.out.println("Argument " + i + " : " + args[i] + "\r\n");
						}
						
                        //csv file containing data
                        //String strFile = "C:/VENUS-C/HakanTirial/SimpleJava/input.csv";

                        //create BufferedReader to read csv file

                        
						
						Boolean shouldWait = Arrays.asList(args).contains("-wait");
						if (shouldWait)
						{
							try{
								int waitTime = Integer.parseInt(GetArg("-wait", args));
								Thread.sleep(waitTime);
							}
							catch(Exception e)
							{
								System.out.println("There is no sleep argument given");
							}
						}

						String infileName = GetArg("-infile", args);
						System.out.println("inputFileName : " + infileName);
						String outfileName = GetArg("-outfile", args);
						Boolean sumSwitch = Arrays.asList(args).contains("-sum");

						BufferedReader br = new BufferedReader( new FileReader(infileName));

                        String strLine = "";

                        StringTokenizer st = null;

                        int lineNumber = 0, tokenNumber = 0, sum = 0;
                        //read comma separated file line by line

                        while( (strLine = br.readLine()) != null)
                        {
                                lineNumber++;
								
                                //break comma separated line using ","

                                st = new StringTokenizer(strLine, ",");

                                while(st.hasMoreTokens())
                                {
                                        //display csv values
                                        tokenNumber++;
                                        //System.out.println("Line # " + lineNumber + 
                                        //                ", Token # " + tokenNumber 
                                        //                + ", Token : "+ st.nextToken());
										
										sum += Integer.parseInt(st.nextToken());
                                }
                                //reset token number

                                tokenNumber = 0;
                        }
						
						System.out.println("The result is " + sum);
						
						
					  BufferedWriter out = new BufferedWriter(new FileWriter(outfileName));
					  out.write(Integer.toString(sum));
					  out.close();

					   System.out.println("Your file has been written");  
 
                }
                catch(Exception e)
                {
                        System.out.println("Exception while reading csv file: " + e);                   
                }
        }
		
		public static String GetArg(String name, String[] args)
		{
			for (int i = 0; i < args.length; i++)
			{
				//System.out.println("name ikilisi " + args[i] + " " + name);
				if (name.equals(args[i])){
					//System.out.println("Olduuuu " + name + " " + args[i]);
					return args[i + 1];
				}
			}

			return "";
		}
          
}
 
