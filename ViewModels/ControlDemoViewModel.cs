﻿using AutoMapper;
using Khsw.Instrument.Demo.Commons.Helper;
using Khsw.Instrument.Demo.DataModels;
using Khsw.Instrument.Demo.Infrastructures;
using Khsw.Instrument.Demo.Models;
using Khsw.Instrument.Demo.Models.Base;
using Khsw.Instrument.Demo.Views.Base;
using System.Collections.ObjectModel;
using System.IO;

namespace Khsw.Instrument.Demo.ViewModels
{
    public class ControlDemoViewModel : BindableBase
    {
        #region Fields
        private readonly IModuleManager _moduleManager;
        private readonly IRegionManager _regionManager;
        private readonly IContainerExtension _container;
        private readonly IDialogService _dialogService;
        private readonly IMapper _mapper;
        private readonly string _prefix = "ControlDemo";
        private readonly string _commandHead = "0xEB90";
        private readonly string _commandEnd = "0xDEAD";
        private readonly string _commandListFileName = $"{nameof(ControlDemoViewModel)}CommandList.xml";
        //private readonly string _commandListFileName = $"{nameof(ControlDemoViewModel)}CommandList.xml";
        private readonly int _defaultLength = 5;
        private object _commandInformationView;
        private int _boardID;
        private InstrumentBase _instrument;
        private UdpInstrument _connectedInstrument;
        #endregion

        #region Properties

        public ObservableCollection<CommandDataModel> CommandList { get; } = new ObservableCollection<CommandDataModel>();

        public string InstrumentManangeRegionName
        {
            get => _prefix + RegionNames.InstrumentManangeRegion;
        }

        public object CommandInformationView
        {
            get => _commandInformationView;
            set => SetProperty(ref _commandInformationView, value);
        }

        public int BoardID
        {
            get { return _boardID; }
            set { SetProperty(ref _boardID, value); }
        }

        public byte[] CommandHeadByte => _commandHead?.ToByteArray();
        public byte[] CommandEndByte => _commandEnd?.ToByteArray();
        #endregion

        #region Commands

        private DelegateCommand _loadingCommand;
        public DelegateCommand LoadingCommand =>
            _loadingCommand ?? (_loadingCommand = new DelegateCommand(ExecuteLoadingCommand));

        private DelegateCommand<CommandDataModel> _sendCommand;
        public DelegateCommand<CommandDataModel> SendCommand =>
            _sendCommand ?? (_sendCommand = new DelegateCommand<CommandDataModel>(ExecuteSendCommand));


        #endregion


        public ControlDemoViewModel(
            IContainerExtension container,
            IModuleManager moduleManager,
            IRegionManager regionManager,
            IDialogService dialogService,
            IMapper mapper)
        {
            _container = container;
            _moduleManager = moduleManager;
            _regionManager = regionManager;
            _dialogService = dialogService;
            _mapper = mapper;
        }

        private void ExecuteLoadingCommand()
        {
            //初始化设备信息
            _instrument = InitInstrumentInfo();

            //初始化指令
            LoadCommandList();

            #region 初始化界面显示
            var parameters = new NavigationParameters();
            parameters.Add("instrument", _instrument);

            _regionManager.RequestNavigate(InstrumentManangeRegionName, nameof(InstrumentManangeView), result =>
            {
                if (result.Success == false)
                {
                    // 输出导航错误的原因
                    var error = result.Exception;
                    Console.WriteLine(error?.Message);
                }
            }, parameters);

            if (_commandInformationView == null)
                CommandInformationView = _container.Resolve<CommandInformationView>();
            #endregion
        }

        internal void SaveData()
        {
            var commandList = _mapper.Map<IList<CommandDataModel>, IList<Command>>(CommandList);

            try
            {
                XmlHelper.SerializeToXml(commandList, _commandListFileName);
            }
            catch (Exception)
            {
                //todo:记录日志保存数据失败
            }
        }

        private UdpInstrument InitInstrumentInfo()
        {
            var instrument = new UdpInstrument();
            instrument.IpAddress = "192.168.10.14";
            instrument.Port = 9000;
            instrument.LocalPort = 12374;
            return instrument;
        }

        private void LoadCommandList()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(dir, _commandListFileName);
            if (File.Exists(filePath))
            {
                var commandList = XmlHelper.DeserializeListFromXml<Command>(filePath);

                if (commandList != null && commandList.Any())
                {
                    foreach (var command in commandList)
                    {
                        CommandList.Add(_mapper.Map<CommandDataModel>(command));
                    }
                }
                else
                {
                    InitCommandList();
                }
            }
            else
            {
                InitCommandList();
            }

        }

        private void InitCommandList()
        {
            var index = 1;

            var command1 = GetStartSignalCommand(index++);
            CommandList.Add(command1);

            var command2 = GetLogicResetCommand(index++);
            CommandList.Add(command2);

            var command3 = GetTBSizeCommand(index++);
            CommandList.Add(command3);

            var command4 = GetCbConfigurationCommand(index++);
            CommandList.Add(command4);

            var command5 = GetFillIn0Command(index++);
            CommandList.Add(command5);

            var command6 = GetLDPCEncodingConfigurationCommand(index++);
            CommandList.Add(command6);

            var command7 = GetLDPCRateMatchingSettingsCommand(index++);
            CommandList.Add(command7);

            var command8 = GetInterweavingSettingsCommand(index++);
            CommandList.Add(command8);

            var command9 = GetScramblingRandomSeedCommand(index++);
            CommandList.Add(command9);

            var command10 = GetFftLengthCommand(index++);
            CommandList.Add(command10);

            var command11 = GetDmrsSettingsCommand(index++);
            CommandList.Add(command11);

            var command12 = GetCpSettingsCommand(index++);
            CommandList.Add(command12);
        }

        private void ExecuteSendCommand(CommandDataModel model)
        {
            try
            {
                var length = BitConverter.GetBytes(model.CommnadLength);
                var id = model.CommandId.ToByteArray();
                var data = string.IsNullOrEmpty(model.CommandContent) ? null : model.CommandContent.ToByteArray();
                var command = GetCommand(length, id, data);

                ((CommandInformationView)_commandInformationView)?.AppendWriteLine(new RecordMessageDataModel()
                {
                    RecordTime = DateTime.Now,
                    RecordMessage = $"发送消息:{command.ToAppendString()}"
                });
            }
            catch (Exception e)
            {
                //todo:记录日志 
                _dialogService.ShowDialog("AlertDialog", new DialogParameters($"message={e.Message}"));
            }


            //#if DEBUG

            //            if (_connectedInstrument == null)
            //            {
            //                var instrumentManage = _container.Resolve<IInstrumentManageService>();
            //                _connectedInstrument = instrumentManage?.GetInstrumentByAddress(_instrument?.Address) as UdpInstrument;

            //                if (_connectedInstrument != null)
            //                {
            //                    _connectedInstrument.SendMessageEvent += (byte[] msg) =>
            //                    {
            //                        ((CommandInformationView)_commandInformationView)?.AppendWriteLine(new RecordMessageDataModel()
            //                        {
            //                            RecordTime = DateTime.Now,
            //                            RecordMessage = $"发送消息:{msg.ToAppendString()}"
            //                        });
            //                    };

            //                    _connectedInstrument.ReceiveMessageEvent += (UdpInstrument instrument) =>
            //                    {
            //                        var message = instrument.GetReceiveMessageFromQueue();
            //                        if (message != null)
            //                            ((CommandInformationView)_commandInformationView)?.AppendWriteLine(new RecordMessageDataModel()
            //                            {
            //                                RecordTime = message.RecordTime,
            //                                RecordMessage = $"接收消息:{message.RecordMessage}"
            //                            });
            //                    };
            //                }
            //            }

            //            if (_connectedInstrument == null)
            //            {
            //                //todo:记录日志 
            //                _dialogService.ShowDialog("AlertDialog", new DialogParameters($"message未能找到已连接的设备"));
            //                return;
            //            }

            //            _connectedInstrument.Send(command);

            //#endif
        }

        public byte[] GetCommand(byte[] length, byte[] cmdID, byte[] data)
        {
            var dataLength = data == null ? 0 : data.Length;
            int len = length.Length + cmdID.Length + dataLength + _defaultLength + 2;

            byte[] cmd = new byte[len];
            int pos = 0;
            Buffer.BlockCopy(CommandHeadByte, 0, cmd, pos, CommandHeadByte.Length);
            pos = pos + CommandHeadByte.Length;

            Buffer.BlockCopy(length, 0, cmd, pos, length.Length);
            pos = pos + length.Length;

            cmd[pos++] = (byte)BoardID;


            Buffer.BlockCopy(cmdID, 0, cmd, pos, cmdID.Length);
            pos += cmdID.Length;

            //占位符
            cmd[pos++] = (byte)0;
            cmd[pos++] = (byte)0;

            if (data != null && data.Any())
            {
                Buffer.BlockCopy(data, 0, cmd, pos, data.Length);
                pos += data.Length;
            }
            Buffer.BlockCopy(CommandEndByte, 0, cmd, pos, CommandEndByte.Length);

            return cmd;
        }

        #region 遥测指令
        /// <summary>
        /// 启动信号
        /// </summary>
        private CommandDataModel GetStartSignalCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "启动信号",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                CommandId = "0x0119"
            };
            return command;
        }

        /// <summary>
        /// 逻辑复位
        /// </summary>
        private CommandDataModel GetLogicResetCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "逻辑复位",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                CommandId = "0x0120"
            };
            return command;
        }

        /// <summary>
        /// TB大小
        /// </summary>
        private CommandDataModel GetTBSizeCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "TB大小",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 4,
                CommandId = "0x0122",
            };
            return command;
        }

        /// <summary>
        /// Cb配置
        /// </summary>
        private CommandDataModel GetCbConfigurationCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "Cb配置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 4,
                CommandId = "0x0124",
            };
            return command;
        }


        /// <summary>
        /// 填0数
        /// </summary>
        private CommandDataModel GetFillIn0Command(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "填0数",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 2,
                CommandId = "0x0125",
            };
            return command;
        }

        /// <summary>
        /// LDPC编码配置
        /// </summary>
        private CommandDataModel GetLDPCEncodingConfigurationCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "LDPC编码配置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 2,
                CommandId = "0x0126",
            };
            return command;
        }

        /// <summary>
        /// LDPC速率匹配设置
        /// </summary>
        private CommandDataModel GetLDPCRateMatchingSettingsCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "LDPC速率匹配设置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 4,
                CommandId = "0x0128",
            };
            return command;
        }

        /// <summary>
        /// 交织设置
        /// </summary>
        private CommandDataModel GetInterweavingSettingsCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "交织设置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 4,
                CommandId = "0x0129",
            };
            return command;
        }

        /// <summary>
        /// 扰码随机种子
        /// </summary>
        private CommandDataModel GetScramblingRandomSeedCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "扰码随机种子",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 8,
                CommandId = "0x012b",
            };
            return command;
        }

        /// <summary>
        /// Fft长度
        /// </summary>
        private CommandDataModel GetFftLengthCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "Fft长度",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 2,
                CommandId = "0x012c",
            };
            return command;
        }

        /// <summary>
        /// Dmrs设置
        /// </summary>
        private CommandDataModel GetDmrsSettingsCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "Dmrs设置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 25,
                CommandId = "0x012d",
            };
            return command;
        }

        /// <summary>
        /// Cp配置
        /// </summary>
        private CommandDataModel GetCpSettingsCommand(int index)
        {
            var command = new CommandDataModel()
            {
                Id = Guid.NewGuid().ToString(),
                Index = index,
                CommandName = "Cp配置",
                CommandHead = _commandHead,
                CommandEnd = _commandEnd,
                ContentEnable = true,
                CommnadLength = 20,
                CommandId = "0x012e",
            };
            return command;
        }
        #endregion
    }
}
