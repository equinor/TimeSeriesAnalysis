function CSVtoTable(tableDiv,csvFilePath)
{

    //var tableTextArray = [['test','test2','test3'],['abc','0.0','1.1']];

    var tableTextArray = CSV2Array(csvFilePath);


    var newDiv = document.createElement("div"); 
    // and give it some content 
    var table = document.createElement("table"); 
    table.className="tableBodyScroll sortable";
    
    var header = table.createTHead();
    var row = header.insertRow(0);     

// Insert a new cell (<td>) at the first position of the "new" <tr> element:
    var headerRowIdx    =0;
    for (var colIdx=0;colIdx<tableTextArray[headerRowIdx].length; colIdx++)
    {
        var cell = row.insertCell(colIdx);
        cell.innerHTML = tableTextArray[headerRowIdx][colIdx];
    }
	var tbody = table.appendChild(document.createElement('tbody'))
    for(var rowIdx =1;rowIdx<tableTextArray.length;rowIdx++)
    {
        // Insert a row in the table at the last row
		var newRow = tbody.appendChild(document.createElement('tr'));
        for (var colIdx=0;colIdx<tableTextArray[rowIdx].length; colIdx++)
        {
            // Insert a cell in the row at index 0
            var newCell  	= newRow.insertCell(colIdx);
			// Append a text node to the cell
            if (colIdx==0 && rowIdx>0)//add link to first column
            {
                var link = document.createElement('a');
                link.setAttribute('href', '\#'+tableTextArray[rowIdx][colIdx]);
                link.innerHTML =tableTextArray[rowIdx][colIdx];
                newCell.appendChild(link);
            }
            else
            {
                var newText  	= document.createTextNode(tableTextArray[rowIdx][colIdx]);
                newCell.appendChild(newText);
                if(tableTextArray[rowIdx][colIdx].charAt(0)=='-')
                    newCell.setAttribute("style","font-style:italic");
                
            }
        }
    }

    // save created table

    newDiv.appendChild(table);  
    // add the newly created element and its content into the DOM 
    var currentDiv = document.getElementById(tableDiv); 
    document.body.insertBefore(newDiv, currentDiv); 

}

function CSV2Array(csvFilePath)
{
    var parsedDataArray;
    var gotResponse = false;
    var waitingTime = 0;
    var waitingTimeIncr = 150;
    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = handleStateChange;
    xhr.open("GET", csvFilePath,false);
    xhr.send();

    return parsedDataArray;

    function handleStateChange() 
    {
        if (xhr.readyState == 4 &&
            xhr.status >= 200 &&
            xhr.status < 300) {
        console.log("Got response for "+csvFilePath);
        gotResponse=true;
        parsedDataArray =  parseDataToArray(xhr.responseText);
        }
    }
    
    
    function parseDataToArray(data) 
    {
        var rows = data.split('\r\n');//data.split(/\s+/);
        var rowNum =rows.length ;
        var cells;
        var cellNum;
        var returnArray = new Array(rowNum);
        for (var rowIdx = 0; rowIdx < rowNum; rowIdx++)
        {
            cells = rows[rowIdx].split(",");
            colNum = cells.length;
            returnArray[rowIdx] = new Array(colNum);
            for(var colIdx=0;colIdx<colNum; colIdx++)
            {
                returnArray[rowIdx][colIdx]  = cells[colIdx];
            }
        }
        return returnArray;
    }
}


