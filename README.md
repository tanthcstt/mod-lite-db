# LiteDB.Studio (Modified)

## Note

The project may encounter errors if any directory contains Unicode characters.

## Features

### 1. Insert Image

   - Syntax: `Insert into DB values {img:"Image(D:\Green.png)"}`
   - The image path is automatically added when the "+Image" button is clicked.
![Screenshot 2024-03-13 112629](https://github.com/tanthcstt/mod-lite-db/assets/127326550/208f0e61-1aaf-4191-aefb-d7bbb6fe9b4e)

### 2. Select Image

   - Syntax: `Select $ from users`
   - All images will be displayed in the "images" result tab.  


### 3. Sort Image by Color (Using OpenCvSharp)

   - Use the drop-down menu in the image result tab to sort images by color.
 - ![Screenshot 2024-03-13 112459](https://github.com/tanthcstt/mod-lite-db/assets/127326550/7e7ae1a6-a67c-4649-91f9-23ab42ec7a96)
   ## 3.1 Open selected file
   ![image](https://github.com/tanthcstt/mod-lite-db/assets/127326550/3fec47ec-2749-4cf4-b486-e8045a7090e7)
   - path auto save in clipboard

### all syntax
select_image $ from TestImg where ddddddddddd;