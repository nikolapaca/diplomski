import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Board } from '../../../core/model/board.model';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BoardActivityPanel } from '../board-activity/board-activity';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CardList } from '../../../core/model/cardList.model';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { CardListService } from '../../../core/service/card-list-service';
import { Card } from '../../../core/model/card.model';
import { CardOverview } from '../card-overview/card-overview';
import { MatCard, MatCardActions, MatCardContent, MatCardHeader, MatCardTitle } from '@angular/material/card';
import { CardListView } from '../../../core/model/card-list-view.model';
import { CardService } from '../../../core/service/card.service';
import { BoardService } from '../../../core/service/board.service';
import { MatMenuModule } from '@angular/material/menu';
import { BoardUpdate } from '../../../core/model/board-update.model';
import { MatDialog } from '@angular/material/dialog';
import { UpdateBoardDialog } from '../../../core/dialog/update-board-dialog/update-board-dialog';
import { AuthService } from '../../../core/service/auth-service';
import { AddCollaboratorsDialog } from '../../../core/dialog/add-collaborators-dialog/add-collaborators-dialog';
import { RemoveCollaboratorDialog } from '../../../core/dialog/remove-collaborator-dialog/remove-collaborator-dialog';
import { BoardMembersDialog } from '../../../core/dialog/board-members-dialog/board-members-dialog';
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
  CdkDrag,
  CdkDropList,
  DragDropModule,
} from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-board-overview',
  imports: [MatDividerModule,
    MatIconModule,
    MatTooltipModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    CdkDropList, 
    DragDropModule,
    CdkDrag,
    MatInputModule,
    MatMenuModule,
    CommonModule,
    MatButtonModule,
    CardOverview,
    MatCardActions,
    MatCard,
    MatCardHeader,
    MatCardContent,
    MatCardTitle,
    MatMenuModule,
    BoardActivityPanel
  ],
  templateUrl: './board-overview.html',
  styleUrl: './board-overview.css'
})
export class BoardOverview implements OnInit {
  public board: Board | undefined;
  public boardOwnerUsername: string | null = null;
  public boardName: string | null = null;
  public highlightedCardId: number | null = null;
  public showAddListForm = false;
  public cardLists: CardListView[] = [];
  public addListForm: FormGroup;
  public errorMessage: string = '';
  public cards: Card[] = [];
  public loggedInUsername = '';
  public isDraggingCard: boolean = false;
  public isDraggingList: boolean = false;
  public currentlyDraggingCard: Card | null = null;
  public currentlyDraggindCardList: CardListView | null = null;


  public constructor(private route: ActivatedRoute,
    private cardListService: CardListService,
    private cardService: CardService,
    private cd: ChangeDetectorRef,
    private router: Router,
    private authService: AuthService,
    private boardService: BoardService,
    private dialog: MatDialog) {
    this.addListForm = new FormGroup({
      name: new FormControl('', Validators.required)
    });
  }

  public ngOnInit(): void {
    this.loggedInUsername = this.authService.getLoggedInUser() || '';
    this.route.queryParamMap.subscribe(params => {
      const cardId = params.get('cardId');
      this.highlightedCardId = cardId ? Number(cardId) : null;
      this.cd.markForCheck();
    });
    this.route.paramMap.subscribe(params => {
      this.boardOwnerUsername = params.get('username');
      this.boardName = params.get('boardName');

      if (this.boardOwnerUsername && this.boardName) {
        this.boardService.getBoardByNameAndOwnerUsername(this.boardName, this.boardOwnerUsername).subscribe({
          next: (board) => {
            this.board = board;
            this.cardListService.getListsForBoard(board.name, board.ownerUsername).subscribe((response: CardList[]) => {
              console.log('ove kartice koje su', response)
              this.cardLists = response.map(list => ({
                ...list,
                showAddCardForm: false,
                addCardForm: new FormGroup({
                  name: new FormControl('', Validators.required),
                  description: new FormControl('', Validators.required)
                }),
                showUpdateListForm: false,
                updateListForm: new FormGroup({
                  name: new FormControl('', Validators.required)
                })
              }));
              this.cd.markForCheck();
            });
          }
        });
      }
    });
  }

  public get connectedLists(): string[] {
  return this.cardLists.map(list => list.id.toString());
}

  // A pinned list/card can only be dropped among other pinned items (top of the
  // block); a non-pinned one can only be dropped after all pinned items. This
  // keeps "pinned always first" true even mid-drag, not just after a refetch.
  public listSortPredicate = (index: number, drag: CdkDrag): boolean => {
    const dragged = this.currentlyDraggindCardList;
    if (!dragged) {
      return true;
    }
    const pinnedCount = this.cardLists.filter(l => l.isPinned).length;
    return dragged.isPinned ? index < pinnedCount : index >= pinnedCount;
  };

  public cardSortPredicate = (index: number, drag: CdkDrag, drop: CdkDropList): boolean => {
    const dragged = this.currentlyDraggingCard;
    if (!dragged) {
      return true;
    }
    const targetCards: Card[] = drop.data || [];
    const pinnedCount = targetCards.filter(c => c.isPinned).length;
    return dragged.isPinned ? index < pinnedCount : index >= pinnedCount;
  };

  public dropList(event: CdkDragDrop<any[]>): void {
    moveItemInArray(this.cardLists, event.previousIndex, event.currentIndex);
    const list: CardList = {
      id: this.currentlyDraggindCardList?.id || 0,
      name: this.currentlyDraggindCardList?.name || '',
      boardName: this.currentlyDraggindCardList?.boardName || '',
      boardUsername: this.currentlyDraggindCardList?.boardUsername || '',
      cards: this.currentlyDraggindCardList?.cards || []
    } 
    this.cardListService.reorderList(list || undefined, event.currentIndex+1).subscribe({
          next: () => {
            this.cardListService.getListsForBoard(this.board?.name, this.board?.ownerUsername).subscribe((response: CardList[]) => {
              console.log('ove kartice koje su', response)
              this.cardLists = response.map(list => ({
                ...list,
                showAddCardForm: false,
                addCardForm: new FormGroup({
                  name: new FormControl('', Validators.required),
                  description: new FormControl('', Validators.required)
                }),
                showUpdateListForm: false,
                updateListForm: new FormGroup({
                  name: new FormControl('', Validators.required)
                })
              }));
              this.cd.markForCheck();
            });
          }
        });
  }

  public onListDragStarted(list:CardListView) : void {
    this.isDraggingList = true;
    this.currentlyDraggindCardList = list;
  }

  public onListDragEnded() : void{
    this.isDraggingList = false;
  }

  public onCardDragStarted(card: Card): void {
    this.isDraggingCard = true;
    this.currentlyDraggingCard = card;
    console.log('Drag started, isDraggingCard:', this.isDraggingCard);
  }

  public onCardDragEnded(): void {
    this.isDraggingCard = false;
  }

  public dropCard(event: CdkDragDrop<any[]>): void {
    console.log('Previous container:', event.previousContainer);
    console.log('Current container:', event.container);
    console.log('Previous container ID:', event.previousContainer.id);
    console.log('Current container ID:', event.container.id);
    console.log('current index:', event.currentIndex);
    console.log('previous index: ', event.previousIndex);
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      this.cardService.reorderCardInsideList(this.currentlyDraggingCard || undefined, event.currentIndex + 1).subscribe({
          next: () => {
            this.cardListService.getListsForBoard(this.board?.name, this.board?.ownerUsername).subscribe((response: CardList[]) => {
              console.log('ove kartice koje su', response)
              this.cardLists = response.map(list => ({
                ...list,
                showAddCardForm: false,
                addCardForm: new FormGroup({
                  name: new FormControl('', Validators.required),
                  description: new FormControl('', Validators.required)
                }),
                showUpdateListForm: false,
                updateListForm: new FormGroup({
                  name: new FormControl('', Validators.required)
                })
              }));
              this.cd.markForCheck();
            });
          }
        });
    } else {
      console.log('prethodni: ', event.previousContainer.data)
      console.log('trenutni: ', event.container.data)
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex,
      );
      const targetList = this.cardLists.find(l => l.id.toString() === event.container.id.replace('list-', ''));
      
      this.cardService.reorderCardOutsideList(this.currentlyDraggingCard || undefined, targetList?.id, event.currentIndex + 1).subscribe({
          next: () => {
            this.cardListService.getListsForBoard(this.board?.name, this.board?.ownerUsername).subscribe((response: CardList[]) => {
              console.log('ove kartice koje su', response)
              this.cardLists = response.map(list => ({
                ...list,
                showAddCardForm: false,
                addCardForm: new FormGroup({
                  name: new FormControl('', Validators.required),
                  description: new FormControl('', Validators.required)
                }),
                showUpdateListForm: false,
                updateListForm: new FormGroup({
                  name: new FormControl('', Validators.required)
                })
              }));
              this.cd.markForCheck();
            });
          }
        });
    }
  }
  
  public closeUpdateListForm(list: CardListView): void {
    list.showUpdateListForm = false;
    list.updateListForm.reset();
  }

  public toggleUpdateListForm(list: CardListView): void {
    list.showUpdateListForm = true;
    list.updateListForm.patchValue({
      name: list.name
    })
  }

  public toggleAddListForm(): void {
    this.showAddListForm = true;
  }

  public closeAddListForm(): void {
    this.showAddListForm = false;

  }

  public clearHighlight(): void {
    this.highlightedCardId = null;
  }

  public toggleAddCardForm(list: CardListView) {
    list.showAddCardForm = true;
  }

  public closeAddCardForm(list: CardListView) {
    list.showAddCardForm = false;
  }

  public async addCard(list: CardListView): Promise<void> {
    if (!list.addCardForm?.valid) {
      return;
    }
    const card: Card = {
      id: 0,
      name: list.addCardForm?.value.name,
      description: list.addCardForm.value.description,
      cardListId: list.id
    }

    this.cardService.addCard(list.id, card).subscribe({
      next: (response) => {

        this.cardService.getCardsByListId(list.id).subscribe({
          next: (response) => {
            list.cards = response;

            list.addCardForm?.reset();
            this.closeAddCardForm(list);

            this.cd.markForCheck();
          }
        });
      }
    });

  }

  private getCardsForList(listId: number) {
    this.cardService.getCardsByListId(listId).subscribe({
      next: (cards: Card[]) => {
        const listToUpdate = this.cardLists.find(l => l.id === listId);
        if (listToUpdate) {
          listToUpdate.cards = cards;
          this.cd.markForCheck();
        }
      }
    });
  }

  public onCardDeleted(listId: number): void {
    this.getCardsForList(listId);
  }

  public onCardUpdated(listId: number): void {
    this.getCardsForList(listId);
  }

  public addCollaborator(): void {
    const dialogRef = this.dialog.open(AddCollaboratorsDialog, {
      width: '1800px',
      data: this.board
    });
  }

  public openMembersDialog(): void {
    if (!this.board) {
      return;
    }
    this.dialog.open(BoardMembersDialog, {
      width: '480px',
      data: this.board
    });
  }

  public removeCollaborator(): void {
    const dialogRef = this.dialog.open(RemoveCollaboratorDialog, {
      width: '1800px',
      data: this.board
    });
  }

  public leaveBoard(){
    this.boardService.leaveBoard(this.board).subscribe({
      next: (_) => {
        this.router.navigate(['home'])
      }
    })
  }

  public addList(): void {
    if (this.board) {
      const cardList: CardList = {
        id: 0,
        name: this.addListForm.value.name,
        boardName: this.board.name,
        boardUsername: this.board.ownerUsername,
        cards: []
      }

      this.cardListService.addList(cardList).subscribe({
      next: (response) => {
        this.showAddListForm = false;
        this.cd.markForCheck();
        this.addListForm.reset();
        if(this.board){
        this.cardListService.getListsForBoard(this.board.name, this.board.ownerUsername).subscribe((response: CardList[]) => {
          this.cardLists = response.map(list => ({
            ...list,
            showAddCardForm: false,
            addCardForm: new FormGroup({
              name: new FormControl('', Validators.required),
              description: new FormControl('', Validators.required)
            }),
            showUpdateListForm: false,
            updateListForm: new FormGroup({
              name: new FormControl('', Validators.required)
            })
          }));
          this.cd.markForCheck();
        });
      }
      }
    });
    }
  }

  public deleteList(list: CardList) {
    this.cardListService.deleteList(list.id).subscribe({
      next: (response) => {
        if(this.board){
        this.cardListService.getListsForBoard(this.board.name, this.board.ownerUsername).subscribe((response: CardList[]) => {
          this.cardLists = response.map(list => ({
            ...list,
            showAddCardForm: false,
            addCardForm: new FormGroup({
              name: new FormControl('', Validators.required),
              description: new FormControl('', Validators.required)
            }),
            showUpdateListForm: false,
            updateListForm: new FormGroup({
              name: new FormControl('', Validators.required)
            })
          }));
          this.cd.markForCheck();
        });
      }
    }});
  }

  public updateList(list: CardListView): void {
    if(this.board){
    const cardList: CardList = {
      id: list.id,
      name: list.updateListForm.value.name,
      boardName: this.board.name,
      boardUsername: this.board.ownerUsername,
      cards: list.cards
    }
    this.cardListService.updateList(cardList).subscribe({
      next: () => {
        list.showUpdateListForm = false;
        if(this.board){
        this.cardListService.getListsForBoard(this.board.name, this.board.ownerUsername).subscribe((response: CardList[]) => {
          this.cardLists = response.map(list => ({
            ...list,
            showAddCardForm: false,
            addCardForm: new FormGroup({
              name: new FormControl('', Validators.required),
              description: new FormControl('', Validators.required)
            }),
            showUpdateListForm: false,
            updateListForm: new FormGroup({
              name: new FormControl('', Validators.required)
            })
          }));
          this.cd.markForCheck();
        });
      }
    }});}
  }
  public updateBoard(): void {
    if(this.board){
    const boardUpdate: BoardUpdate = {
      oldBoardName: this.board.name,
      newBoardName: '',
      oldBoardDescription: this.board.description,
      newBoardDescription: '',
      ownerUsername: this.board.ownerUsername
    }
    
    const dialogRef = this.dialog.open(UpdateBoardDialog, {
      width: '1800px',
      data: boardUpdate
    });
    

    dialogRef.componentInstance.boardSubmitted.subscribe((board: BoardUpdate) => {
      this.boardService.updateBoard(board).subscribe({
        next: () => {
          this.boardService.refreshState();
          this.router.navigate([`boards/${board.ownerUsername}/${board.newBoardName}`])
          dialogRef.close();
        },
        error: (error) => {
          let errorMessage = 'Something went wrong. Please try again.';
          if (error.error && typeof error.error === 'string') {
            errorMessage = error.error;
          } else if (error.error && error.error.errors) {
            const firstError = Object.values(error.error.errors)[0] as string[];
            if (firstError && firstError.length > 0) {
              errorMessage = firstError[0];
            }
          }
          dialogRef.componentInstance.setBackendError(errorMessage);
        }
      });
    });
  }}

  public deleteBoard(): void {
    this.boardService.deleteBoard(this.boardName, this.boardOwnerUsername).subscribe((_) => {
      this.boardService.refreshState();
      this.router.navigate([`home`])
    })
  }

  public archiveBoard(): void {
    if (!this.boardName || !this.boardOwnerUsername) {
      return;
    }
    this.boardService.archiveBoard(this.boardName, this.boardOwnerUsername).subscribe((_) => {
      this.router.navigate([`home`])
    })
  }

  public toggleFavorite(): void {
    if (!this.board) {
      return;
    }
    this.board.isFavorite = !this.board.isFavorite;
    this.boardService.toggleFavorite(this.board.name, this.board.ownerUsername).subscribe({
      error: () => {
        if (this.board) {
          this.board.isFavorite = !this.board.isFavorite;
        }
      }
    });
  }

  public togglePinList(list: CardListView): void {
    this.cardListService.togglePin(list.id).subscribe({
      next: () => {
        if (this.board) {
          this.cardListService.getListsForBoard(this.board.name, this.board.ownerUsername).subscribe((response: CardList[]) => {
            this.cardLists = response.map(list => ({
              ...list,
              showAddCardForm: false,
              addCardForm: new FormGroup({
                name: new FormControl('', Validators.required),
                description: new FormControl('', Validators.required)
              }),
              showUpdateListForm: false,
              updateListForm: new FormGroup({
                name: new FormControl('', Validators.required)
              })
            }));
            this.cd.markForCheck();
          });
        }
      }
    });
  }
}