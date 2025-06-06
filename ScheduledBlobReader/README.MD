
#### To Excecute Locally

`func start` -- starts the functions locally

- It starts the Azure Functions runtime locally.
- It scans your project directory for all subfolders that contain a function.json file.
- For each folder with a valid function.json, it loads and runs that function.
- It listens for triggers (like HTTP requests, timers, queues, etc.) defined in those files.

`netstat -ano | findstr :7071`   -- lists processes and ports in use

`taskkill /PID 25388 /F`   -- kills the task with given ID

`func init ScheduledBlobReader --worker-runtime dotnet` -- intialises a function

`func new --template "Timer trigger" --name ScheduledBlobReader` --Creates a Function with Timer Trigger

`func new --template "HTTP trigger" --name ManualBlobReader`   --Creates a Function with Http Trigger  



