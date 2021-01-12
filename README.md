# NETFastSearchLibrary
Es una biblioteca multiproceso de .NET 4.0 escrita en C# que proporciona la capacidad de encontrar rápidamente archivos o directorios 
utilizando diferentes criterios de búsqueda.

## VENTAJA
* Es un algoritmo de búsqueda recursivo que se divide en subtareas que se ejecutan en el grupo de subprocesos.
* Es compatible con Windows XP y superior.
* A diferencia de otros buscadores, la "UnauthorizedAccessException" nunca se inicia mientras se ejecuta la búsqueda.
* Es posible elegir diferentes criterios de búsqueda.
* Es posible detener el proceso de búsqueda cuando sea necesario.
* Es posible establecer diferentes rutas de búsqueda al mismo tiempo.

## REQUERIMIENTOS E INSTALACIÓN
1. Descargue el archivo con la última [versión] (https://github.com/PaatyDSM/NETFastSearchLibrary/releases "Última versión").
2. Extraiga el contenido a un directorio.
3. Copie el archivo .dll en el directorio de su proyecto.
4. Agregue la biblioteca a su proyecto: En el Explorador de soluciones haga click derecho sobre el nombre de la solución, 
   "Agregar -> Referencia..." en el menú contextual, luego -> "Examinar", busque la librería .dll extraída, y selecciónela.
5. Establezca la versión de .NET de destino al menos como ".NET Framework 4": En el Explorador de soluciones haga click derecho 
   sobre el nombre de la solución, "Propiedades" en el menú contextual, luego en la solapa "Aplicación" establezca la plataforma de destino 
   como mínimo en ".NET Framework 4" y guarde los cambios. (Nota: .NET Framework 4 es compatible con Windows XP en adelante).
6. Cuando necesite utilizar las funciones de la librería recuerde agregar el espacio de nombres apropiado: 
   "using NETFastSearchLibrary;" al principio del archivo clase.


## NOTAS DE VERSIÓN
V1.0.2.0:
    *Paquetes Nuget actuallizados
    *Licencia actualizada.

## CONTENIDO
Las siguientes clases proporcionan funcionalidad de búsqueda:
* FileSearch
* DirectorySearch
* FileSearchMultiple
* DirectorySearchMultiple

## PRINCIPIOS DE USO
* Las clases "FileSearch" y "DirectorySearch", contienen métodos estáticos que permiten ejecutar búsquedas de archivos y directorios 
respectivamente con diferentes criterios de búsqueda. Estos métodos devuelven resultados solo cuando completan la ejecución por completo.
* Los métodos que terminan con la palabra "Fast" al final, dividen la tarea en varias subtareas que se ejecutan simultáneamente 
  en el grupo de subprocesos para acelerar la búsqueda.
* Los métodos que terminan con la palabra "Async" al final devuelven una Tarea (return Task) y no bloquean el hilo (thread) llamado.
* Este primer grupo de métodos acepta 2 parámetros:
  * "string folder" - Especifica el directorio de inicio de búsqueda.
  * "string pattern" - Esta es la cadena de búsqueda para hacer coincidir con los nombres de los archivos en a buscar en la ruta.
    Este parámetro puede contener una combinación de ruta literal válida y caracteres comodín (* y ?) pero no admite expresiones regulares.

## EJEMPLOS:

* Finds all *.txt files in C:\Users using one thread method.
  
  List<FileInfo> files = FileSearcher.GetFiles(@"C:\Users", "*.txt");
  
* Finds all files that match appropriate pattern using several threads in thread pool.
  
  List<FileInfo> files = FileSearcher.GetFilesFast(@"C:\Users", "*SomePattern*.txt");
  
* Finds all files that match appropriate pattern using several threads in thread pool as an asynchronous operation.
  
  Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(@"C:\", "a?.txt");
  


* Second group of methods accepts 2 parameters:
     * `string folder` - start search directory
     * `Func<FileInfo, bool> isValid` - delegate that determines algorithm of file selection.
     
   Examples:
   
    Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(@"D:\", (f) =>
    {
         return (f.Name.Contains("Pattern") || f.Name.Contains("Pattern2")) &&
                 f.LastAccessTime >= new DateTime(2018, 3, 1) && f.Length > 1073741824;
    });
   Finds all files that match appropriate conditions using several threads in thread pool as
   an asynchronous operation.
   
   You also can use regular expressions:
    
    Task<List<FileInfo>> task = FileSearcher.GetFilesFastAsync(@"D:\", (f) =>
    {
         return (f) => Regex.IsMatch(f.Name, @".*Imagine[\s_-]Dragons.*.mp3$");
    }); 
    
   Finds all files that match appropriate regular expression using several thread in thread pool as
   an asynchronous operation.
   
### Opciones avanzadas
   If you want to execute some complicated search with realtime result getting you should use instance of `FileSearcher` class,
   that has various constructor overloads.
   `FileSearcher` class includes next events:
   * `event EventHandler<FileEventArgs> FilesFound` - fires when next portion of files is found.
     Event includes `List<FileInfo> Files { get; }` property that contains list of finding files.
   * `event EventHandler<SearchCompleted> SearchCompleted` - fires when search process is completed or stopped. 
     Event includes `bool IsCanceled { get; }` property that contains value that defines whether search process stopped by calling
     `StopSearch()` method. 
    To get stop search process possibility one has to use constructor that accepts CancellationTokenSource parameter.
    
   Example:
    
    class Searcher
    {
        private static object locker = new object(); // locker object

        private FileSearcher searcher;

        List<FileInfo> files;

        public Searcher()
        {
            files = new List<FileInfo>(); // create list that will contain search result
        }

        public void StartSearch()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            // create tokenSource to get stop search process possibility

            searcher = new FileSearcher(@"C:\", (f) =>
            {
               return Regex.IsMatch(f.Name, @".*[iI]magine[\s_-][dD]ragons.*.mp3$"); 
            }, tokenSource);  // give tokenSource in constructor
 

            searcher.FilesFound += (sender, arg) => // subscribe on FilesFound event
            {
                lock (locker) // using a lock is obligatorily
                {
                    arg.Files.ForEach((f) =>
                    {
                        files.Add(f); // add the next part of the received files to the results list
                        Console.WriteLine($"File location: {f.FullName}, \nCreation.Time: {f.CreationTime}");
                    });

                    if (files.Count >= 10) // one can choose any stopping condition
                       searcher.StopSearch();
                }
            };

            searcher.SearchCompleted += (sender, arg) => // subscribe on SearchCompleted event
            {
                if (arg.IsCanceled) // check whether StopSearch() called
                    Console.WriteLine("Search stopped.");
                else
                    Console.WriteLine("Search completed.");

                Console.WriteLine($"Quantity of files: {files.Count}"); // show amount of finding files
            };

            searcher.StartSearchAsync();
            // start search process as an asynchronous operation that doesn't block the called thread
        }
    }
 Note that all `FilesFound` event handlers are not thread safe so to prevent result loosing one should use
 `lock` keyword as you can see in example above or use thread safe collection from `System.Collections.Concurrent` namespace.
 
### Opciones extendidas
   There are 2 additional parameters that one can set. These are `handlerOption` and `suppressOperationCanceledException`.
   `ExecuteHandlers handlerOption` parameter represents instance of `ExecuteHandlers` enumeration that specifies where
   FilesFound event handlers are executed:  
   * `InCurrentTask` value means that `FileFound` event handlers will be executed in that task where files were found. 
   * `InNewTask` value means that `FilesFound` event handlers will be executed in new task.
    Default value is `InCurrentTask`. It is more preferably in most cases. `InNewTask` value one should use only if handlers execute
    very sophisticated work that takes a lot of time, e.g. parsing of each found file.
    
   `bool suppressOperationCanceledException` parameter determines whether necessary to suppress 
   OperationCanceledException.
   If `suppressOperationCanceledException` parameter has value `false` and StopSearch() method is called the `OperationCanceledException` 
   will be thrown. In this case you have to process the exception manually.
   If `suppressOperationCanceledException` parameter has value `true` and StopSearch() method is called the `OperationCanceledException` 
   is processed automatically and you don't need to catch it. 
   Default value is `true`.
   
   Example:
            
    CancellationTokenSource tokenSource = new CancellationTokenSource();

    FileSearcher searcher = new FileSearcher(@"D:\Program Files", (f) =>
    {
       return Regex.IsMatch(f.Name, @".{1,5}[Ss]ome[Pp]attern.txt$") && (f.Length >= 8192); // 8192b == 8Kb 
    }, tokenSource, ExecuteHandlers.InNewTask, true); // suppressOperationCanceledException == true
    
### BÚSQUEDA MÚLTIPLE
   `FileSearcher` and `DirectorySearcher` classes can search only in one directory (and in all subdirectories surely) 
   but what if you want to perform search in several directories at the same time?     
   Of course, you can create some instances of `FileSearcher` (or `DirectorySearcher`) class and launch them simultaneously, 
   but `FilesFound` (or `DirectoriesFound`) events will occur for each instance you create. As a rule, it's inconveniently.
   Classes `FileSearcherMultiple` and `DirectorySearcherMultiple` are intended to solve this problem. 
   They are similar to `FileSearcher` and `DirectorySearcher` but can execute search in several directories.
   The difference between `FileSearcher` and `FileSearcheMultiple` is that constructor of `Multiple` class accepts list of 
   directories instead one directory.
   
   Example:
   
    List<string> folders = new List<string>
    {
      @"C:\Users\Public",
      @"C:\Windows\System32",
      @"D:\Program Files",
      @"D:\Program Files (x86)"
    }; // list of search directories

    List<string> keywords = new List<string> { "word1", "word2", "word3" }; // list of search keywords

    FileSearcherMultiple multipleSearcher = new FileSearcherMultiple(folders, (f) =>
    {
       if (f.CreationTime >= new DateTime(2015, 3, 15) &&
          (f.Extension == ".cs" || f.Extension == ".sln"))
          {
             foreach (var keyword in keywords)
               if (f.Name.Contains(keyword))
                 return true;
          }
          
       return false;
    }, tokenSource, ExecuteHandlers.InCurrentTask, true);       

### NOTAS
   #### Using "await" keyword
   It is highly recommend to use "await" keyword when you use any asynchronous method. It allows to get possible
   exceptions from method for following processing, that is demonstrated next code example. Error processing in previous 
   examples had been missed for simplicity.

  Example:

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using FastSearchLibrary;

    namespace SearchWithAwait
    {
        class Program
        {
           private static object locker = new object();

           private static List<FileInfo> files;

           private static Stopwatch stopWatch;


           static void Main(string[] args)
           {
              string searchPattern = @"\.mp4$";

              StartSearch(searchPattern);

              Console.ReadKey(true);
           }


           private static async void StartSearch(string pattern)
           {
              stopWatch = new Stopwatch();

              stopWatch.Start();

              Console.WriteLine("Search had been started.\n");

              files = new List<FileInfo>();

              List<string> searchDirectories = new List<string>
              {
                   @"C:\",
                   @"D:\"
              }; 

              FileSearcherMultiple searcher = new FileSearcherMultiple(searchDirectories, (f) =>
              {
                  return Regex.IsMatch(f.Name, pattern);
              }, new CancellationTokenSource());

              searcher.FilesFound += Searcher_FilesFound;
              searcher.SearchCompleted += Searcher_SearchCompleted;

              try
              {
                 await searcher.StartSearchAsync();
              }
              catch (AggregateException ex)
              {
                 Console.WriteLine($"Error occurred: {ex.InnerException.Message}");
              }
              catch (Exception ex)
              {
                 Console.WriteLine($"Error occurred: {ex.Message}");
              }
              finally
              {
                 Console.Write("\nPress any key to continue...");
              }
           } 


           private static void Searcher_FilesFound(object sender, FileEventArgs arg)
           {
              lock (locker) // using a lock is obligatorily
              {
                 arg.Files.ForEach((f) =>
                 {
                    files.Add(f); // add the next part of the received files to the results list
                    Console.WriteLine($"File location: {f.FullName}\nCreation.Time: {f.CreationTime}\n");
                 });
              }
           }


           private static void Searcher_SearchCompleted(object sender, SearchCompletedEventArgs arg)
           {
              stopWatch.Stop();

              if (arg.IsCanceled) // check whether StopSearch() called
                Console.WriteLine("Search stopped.");
              else
                Console.WriteLine("Search completed.");

              Console.WriteLine($"Quantity of files: {files.Count}"); // show amount of finding files

              Console.WriteLine($"Spent time: {stopWatch.Elapsed.Minutes} min {stopWatch.Elapsed.Seconds} s {stopWatch.Elapsed.Milliseconds} ms");
           }
        }
    }

#### Long paths Windows limitation
Una clave de registro permite habilitar o deshabilitar el nuevo comportamiento de ruta larga en Windows. Para habilitar el comportamiento de ruta larga, abra el editor de registro y siga la siguiente ruta: 'HKLM\SYSTEM\CurrentControlSet\Control\FileSystem'.
Luego cree el parámetro: 'LongPathsEnabled' (escriba REG_DWORD) con el valor '1' y reinicie su computadora.

### VELOCIDAD DE TRABAJO
It depends on your computer performance, current loading, but usually `Fast` methods and instance method `StartSearch()` are
performed at least in 2 times faster than simple one-thread recursive algorithm if you use modern multicore processor of course.