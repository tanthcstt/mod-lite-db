# LiteDB.Studio (Modified)

## Note

The project may encounter errors if any directory contains Unicode characters.

## Installer
[LiteDBStudio.zip](https://github.com/tanthcstt/mod-lite-db/files/15283713/LiteDBStudio.zip)


### all syntax

## Insert Image
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\1.jpg)", mssv:22521300};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\2.jpg)", mssv:22521339};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\3.jpg)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\4.png)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\5.png)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\6.jpg)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\7.png)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\8.png)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\9.jpg)"};
insert_img into TestImg values {aa:"Image(E:\\LiteDBTestImg\\10.jpg)"};



## select image by text description
select_image $ from TestImg where "a man with pickaxe";
select_image $ from TestImg where "a man with sword";
select_image $ from TestImg where "an apple";
select_image $ from TestImg where "a dog";







## test clear tmp data when delete/drop collection
delete TestImg where mssv=22521300;
