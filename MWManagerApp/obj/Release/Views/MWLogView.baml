   M S B A M L   `   `   `  ���� O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  G CMWManagerApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:MWManagerApp.ViewsMWManagerApp.Views ;"clr-namespace:MWManagerApp.HelpersMWManagerApp.Helpers A%clr-namespace:MWManagerApp.ViewModelsMWManagerApp.ViewModels "   MWManagerApp.Views.MWLogView   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     6sys&clr-namespace:System;assembly=mscorlib     2local clr-namespace:MWManagerApp.Views     6helpers"clr-namespace:MWManagerApp.Helpers     4vm%clr-namespace:MWManagerApp.ViewModels     -     N�Name$  LogPage�� N� Title$ LogView��$��Stretch=�$��Stretch=�$
��auto��$
��auto�� � DataContext ,  &MWManagerApp.ViewModels.MWLogViewModel   � 	Resources ��%    .��� $��10,10,10,10q� � ColumnDefinitions �� $
��Auto ��� $��* � � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto ��� .+��� $��0��$��0������ $
��Bold"�.�구독 조건.���� 
�������
�������$��1q�._���  *  $MWManagerApp.Helpers.EnableConverter   
enableConv��%   &	          .+�� -   $��wpConfig��$��0,0,0,10q�$��Center=�{��� *SubscribeStatus.IsRunning+ �� 	Converter# [  .+��� $
��Bold"�.��
아이피:�� -    ��Name$ txtHostName�� �� TextAlignment$ Justify=�$��Center=�$��80������ *Config.HostName+�� $
��Bold"�.��
아이디:�� -   $ txtID��$ Justify=�$��Center=�$��60������ *	Config.ID+�� $
��Bold"�.��패스워드:�� -   $ txtPW��$ Justify=�$��Center=�$��60������ *	Config.PW+� $��Center=� �� $
��Auto ��� $��* � �� $
��Auto �.+�� -   $��
wpLogQueue��$��0q�$��0��{��� *SubscribeStatus.IsRunning+# [  .+��� $
��Bold"�.��구독 큐:�� -   $��listLogQueue������ *MWQueueCollection+���� .R�� 	 K� IsItemsHost	 . $	��300������ .R��� 
 ��Name$
 chkLogQueue�� d� 	IsChecked �� *
IsSelected+ �� Mode$ TwoWay=����� *Name+
 � Tag �� *Name+� $��1��$��0q�$��Right=�$��Center=�.+��� $
��Bold"�.��
상태 :�� -    ��Name$ 	txtStatus��$	��100�� �� TextAlignment$ Justify=�$��Center=�$
��Bold"�
���  ������ *SubscribeStatus.Text+�� -	    ��Name$ btnRun��$��실행��$��55��$��5,0,5,0q�$��Center=�$��20��{��� *SubscribeStatus.IsRunning+# [  �� -
   $ btnStop��$��중지��$��55��$��0,0,5,0q�$��20��$��Center=��� -   $ btnSubscribeListViewClear��$��목록지우기��$��80��$��0,0,5,0q�$��20��$��Center=�� $��0,0,0,10q�$��Center=��� $��1��$��0������ $
��Bold"�.�구독 목록.���� 
�������
�������$��1q�._��� -    ��Name$ SubscribeListView��$��Stretch=�$��Stretch=�$ �� IsSynchronizedWithCurrentItem$
 TrueY�$��Visible=���.  $
��Auto��$	��200�� ��  �� RelativeSource ��  �� Mode$
 Self=����� *SubscribeCollection+ &   MWManagerApp.Helpers.BGConverter 
 BGConv+  %MWManagerApp.Helpers.NewLineConverter 
 NLConv&   MWManagerApp.Helpers.FGConverter 
 FGConv �� 
TargetType �� Property �� Value �� Path��%�   &	       &	      &	 
     '��      7  7        �� " ��.���  � �� �� 8   ��  � �� �� 8  ��  �@�ToolTip �� $ Info.Payload� �� View �� .P��� $	��seq�� �� Width$ 80�� �� DisplayMemberBinding �� $ Info.Seq� �� StringFormat$ {0:#,#}���� $��routing key��$	 120�� �� $ Info.RoutingKey��� $��payload��$	 400�� �� CellTemplate �� .R��� $��20��$��CharacterEllipsis=����� $ Info.Payload�# [ �� $��ins date��$	 150�� �� $ Info.InsDate�$! {0:yyyy-MM-dd HH:mm:ss.fff}���� $��2��$��0������ $
��Bold"�.�조회 조건.���� 
�������
�������$��1q�._��� .+��  �� $
��Auto ��� $��* � �� $
��Auto �.+��� $��0q�$��0��.+�� $	��0,5q�.+��� .��
기간 :(  "System.Windows.Controls.DatePicker  -     Name$ dtBeginDate��
 �� Now    SelectedDate#  Z $	��100���� .��~  -   $ 	dtEndDate��#  Z $	��100��� $y�	Collapsed=�.+��� .��seq:�� -   $ txtSeq��$�� ��$��50��$��RightToLeft=�$��Center=�$��Stretch=��� .��routing key:�� -   $ txtRoutingKey��$	��200���� .��조회건수:�� -   $ txtLimit��$��30��$��20��$��10,0,0,0q�$��RightToLeft=�$��Center=�$��Stretch=�� $��	0,0,10,10q�$��1��$��Bottom=�$��Right=�.+��� -   $ 	btnSearch��$��조회��$��47��$��5,0,0,0q�$
��Left=��� -   $ btnLogListViewClear��$��목록지우지��$��80��$��5,0,0,0q�$
��Left=��� $��3��$��0������ $
��Bold"�.�로그 목록.���� 
�������
�������$��1q�._��� -   $ LogListView��$
 TrueY�$��Stretch=�$��Stretch=�$
��Auto=���.   ��  �� $
 Self=����� *LogCollection+ ��%�   &	       &	      &	 
     '��      7  7        �� " ��.���  � �� �� 8   ��  � �� �� 8   �� .P��� $	��seq��$ 80�� �� $ Info.Seq�$ {0:#,#}���� $��routing key��$	 120�� �� $ Info.RoutingKey��� $��payload��$	 400�� �� .R��� $��CharacterEllipsis=�$��20������ $ Info.Payload�# [ �� $��ins date��$	 150�� �� $ Info.InsDate�$! {0:yyyy-MM-dd HH:mm:ss.fff}���� $��1��$��4��$��Stretch=�$��Stretch=�$
��Auto=���.  .���� ���� $
��Bold"�$�로그 상세��.���� 
�������$��1q�._���  .  (MWManagerApp.Helpers.MultiElementConvert  multiElementConverter��%   &	          `� ! `� 	Converter#! [ .1��� " �� ElementName$" LogPage��$
 Mode��� $" SubscribeListView��$ SelectedItem��� $" LogListView��$ SelectedItem�.+��� $��1q�
���   ����� $
��Bold"�$	�seq��.���� $��Center=����� *
Info.Seq+$ OneWay=�$ {0:#,#}���� $��1q�
���   ����� $
��Bold"�$�exchange��.���� $��Bottom=�$��0,0,0,1q����� *Info.Exchange+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�routing_key.���� $��Center=����� *Info.RoutingKey+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�queue.���� $��Center=����� *
Info.Queue+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�consumer_tag.���� $��Center=����� *Info.ConsumerTag+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�deliver_tag.���� $��Center=����� *Info.DeliverTag+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�	headers.���� # �� 
IsReadOnly# . $��Stretch=�$ �� TextWrapping$
$ Wrap=�$ Justify=�$��0q�$��Disabled=�$
��Auto=���.  $
��Auto��$��50��$	��250������ *Prop.HeadersJSON+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�	payload.���� # . $��Stretch=�$��0q�$
$ Wrap=�$ Justify=�$��Disabled=�$
��Auto=���.  $
��Auto��$��50��$	��250������ *Info.Payload+$ OneWay=��� $��1q�
���   ����� $
��Bold"�.�
ins_date.���� $��Center=�$
��Wrap=�$ Justify=����� *Info.InsDate+$ OneWay=�$! {0:yyyy-MM-dd HH:mm:ss.fff}��