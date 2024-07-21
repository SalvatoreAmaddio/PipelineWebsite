# PipelineWebsite
This console line application is an Ad-hoc Solution for a Client of mine who needed to extract data from their website and print them onto a spreadsheet.
For privacy purposes, some information has been omitted such as:
- Login Details
- Website Links
- Records information to retrieve.

Therefore, if you would try to debug this code, it won't work.

Yet you can see how I have approached the problem.

# Approach to Bottle-Neck:
Loading a single page takes approximately 2 minutes and 30 seconds. With a total of 258 pages, this process would take around 9 hours. To prevent this lengthy task from occurring each time the application runs, I have developed an SQLite database to store all records. The whole dataset was fetched during the production stage.

The release checks the number of records in the database against the number of records displayed on the webpage. This allows the application to fetch only the new records and insert them into the database. As a result, execution time is reduced by 95%, ensuring that the client always has access to the complete dataset, including the latest entries.

# Flow Chart
![Alt text](https://github.com/SalvatoreAmaddio/PipelineWebsite/blob/main/static/Flow-Chart.png)
