auxmic
http://auxmic.com

Copyright © 2014-2020 Vladislav Osmanov
info@auxmic.com


INTRODUCTION
------------
auxmic is a free audio synchronization software. 
The only purpose is to help you synchronize audio from external microphone with video.


RELEASE NOTES
-------------
auxmic v0.8.1.115 [2020-08-09]

  + Improved usability (#6)
  + Added ability to export synchronized file with `FFmpeg` (#7)
  + Fixed multiple instances run (#11)
  + Fixed progress display (while matching files) (#17)

auxmic v0.8.0.103 [2020-06-05]

  + Fixed crash on processing files with the same names (but different extensions) #1
  + Fixed application hang while exporting synched audio #2
  + Fixed ArgumentOutOfRangeException while exporting synced file with negative offset #5
  + NAudio updated to v1.10.0

auxmic v0.8.0.XXXXXXX [2018-02-21]

  + fixed saving of the matched file
  + updated NAudio library to 1.8.4

auxmic v0.8.0.97 [2017-01-21]

  + new matching algorithm: handles negative offset from master audio file
  + fixed cache files mismatch for audio sources with different sample rates
  + updated NAudio library to 1.8.0

auxmic v0.7.0.83 [2014-10-27]

  + multiple files processing
  + new GUI
  + menu commands to view and clear cache


auxmic v0.6 [2014-09-29]

  + significantly reduced the amount of RAM required for files processing


auxmic v0.5 [2014-09-20]

  + NAudio by Mark Heath [http://naudio.codeplex.com/]
    + more media formats supported: audio and video containers
    + built in resampling


auxmic v0.4 [2014-08-30]

  + first public release
  + supports only WAVE-audio files 8- and 16-bit PCM 