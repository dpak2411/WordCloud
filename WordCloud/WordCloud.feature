Feature: WordCloud
	
Background:
  Given alteryx running at" http://gallery.alteryx.com/"
  And I am logged in using "deepak.manoharan@accionlabs.com" and "P@ssw0rd"

Scenario Outline: Run the word cloud app
When I run the app "<app>" that makes the word cloud with the text "<text>"
And I give the imagewidth <width> and height <height> and resolution <resolution>
And I also give the sequential colors <nocolors> and the palette "<palette>"
Then I see the there is an image file as output "<output>"
Examples: 
| app            | text                    | width | height | resolution | nocolors | palette | output             |
| Word Cloud App | This is a wonderful app to be tested | 1600  | 1600   | 300        | 7        | Blues   | WordCloud.png      |



